using Microsoft.Extensions.Options;
using NaHora.Domain.RealTime;
using NaHora.Infrastructure.RealTime;
using NaHora.Shared.Configuration;

namespace NaHora.UnitTests.RealTime;

/// <summary>
/// The registry that decides who sees what.
/// </summary>
/// <remarks>
/// No delays anywhere. Registration completes synchronously before the first
/// read parks, and broadcasting is synchronous, so every assertion here is
/// about a state the code has definitely reached rather than one it probably
/// reached in time.
/// </remarks>
public sealed class RealTimeObserverRegistryTests
{
    [Fact]
    public async Task Broadcast_ReachesTheObserversOfThatCompanyOnly()
    {
        // Arrange — two observers, two companies, one event
        var registry = CreateRegistry();
        var mine = Guid.CreateVersion7();
        var theirs = Guid.CreateVersion7();

        await using var subscriber = Observe(registry, mine);
        await using var outsider = Observe(registry, theirs);

        // Act
        await registry.BroadcastAsync(EventFor(mine), TestContext.Current.CancellationToken);

        // Assert
        (await subscriber.Pending).ShouldBeTrue();
        subscriber.Enumerator.Current.CompanyId.ShouldBe(mine);

        // Broadcasting is synchronous, so an event that has not arrived by now
        // is an event that was never addressed here.
        outsider.Pending.IsCompleted.ShouldBeFalse(
            "an observer must never see another company's events");
    }

    [Fact]
    public async Task Broadcast_WhenTheObserverIsBehind_KeepsTheNewestAndDropsTheOldest()
    {
        // Arrange — a buffer of two, and four events with nobody reading
        var registry = CreateRegistry(bufferSize: 2);
        var companyId = Guid.CreateVersion7();

        await using var subscriber = Observe(registry, companyId);

        await registry.BroadcastAsync(EventFor(companyId, "first"), TestContext.Current.CancellationToken);
        (await subscriber.Pending).ShouldBeTrue();

        // Act — three more while no read is outstanding, so they queue
        foreach (var type in (string[])["second", "third", "fourth"])
        {
            await registry.BroadcastAsync(EventFor(companyId, type), TestContext.Current.CancellationToken);
        }

        // Assert — the observer resumes at the newest, not at the backlog
        (await subscriber.Enumerator.MoveNextAsync()).ShouldBeTrue();
        subscriber.Enumerator.Current.Type.ShouldBe("third");

        (await subscriber.Enumerator.MoveNextAsync()).ShouldBeTrue();
        subscriber.Enumerator.Current.Type.ShouldBe("fourth");
    }

    [Fact]
    public async Task Broadcast_WithNobodyConnected_DoesNothing()
    {
        // Arrange — the normal state outside business hours
        var registry = CreateRegistry();

        // Act
        var broadcast = async () => await registry.BroadcastAsync(
            EventFor(Guid.CreateVersion7()),
            TestContext.Current.CancellationToken);

        // Assert
        await broadcast.ShouldNotThrowAsync();
    }

    [Fact]
    public async Task Observe_WhenTheCallerGoesAway_EndsTheStream()
    {
        // Arrange
        var registry = CreateRegistry();
        var companyId = Guid.CreateVersion7();
        using var disconnect = new CancellationTokenSource();

        var enumerator = registry.ObserveAsync(companyId, disconnect.Token).GetAsyncEnumerator(disconnect.Token);
        var pending = enumerator.MoveNextAsync().AsTask();

        // Act
        await disconnect.CancelAsync();

        // Assert — the enumeration terminates, which is what releases the
        // registration in the iterator's finally block
        await Should.ThrowAsync<OperationCanceledException>(async () => await pending);
    }

    private static InMemoryRealTimeObserverRegistry CreateRegistry(int bufferSize = 32) =>
        new(Options.Create(new RealTimeOptions { ObserverBufferSize = bufferSize }));

    private static RealTimeEvent EventFor(Guid companyId, string type = "test.event") =>
        RealTimeEvent.Create(type, companyId, DateTimeOffset.UtcNow);

    /// <summary>
    /// Starts observing and returns once the registration is in place.
    /// </summary>
    /// <remarks>
    /// The iterator body runs synchronously up to the first read that has to
    /// wait, so by the time MoveNextAsync hands back an incomplete task the
    /// observer is registered. Awaiting that task instead would wait for an
    /// event that has not been sent yet.
    /// </remarks>
    private static Subscriber Observe(InMemoryRealTimeObserverRegistry registry, Guid companyId)
    {
        var disconnect = new CancellationTokenSource();
        var enumerator = registry.ObserveAsync(companyId, disconnect.Token).GetAsyncEnumerator(disconnect.Token);

        return new Subscriber(disconnect, enumerator, enumerator.MoveNextAsync().AsTask());
    }

    /// <summary>
    /// One connected observer, disconnected the way a real one is.
    /// </summary>
    /// <remarks>
    /// Disposal cancels first and only then disposes the enumerator: an
    /// observer waiting for an event it will never receive still has a read
    /// outstanding, and disposing an enumerator mid-read throws.
    /// </remarks>
    private sealed class Subscriber(
        CancellationTokenSource disconnect,
        IAsyncEnumerator<RealTimeEvent> enumerator,
        Task<bool> pending) : IAsyncDisposable
    {
        public IAsyncEnumerator<RealTimeEvent> Enumerator => enumerator;

        public Task<bool> Pending => pending;

        public async ValueTask DisposeAsync()
        {
            await disconnect.CancelAsync();

            try
            {
                await pending;
            }
            catch (OperationCanceledException)
            {
                // Expected: this is what disconnecting looks like from inside.
            }

            await enumerator.DisposeAsync();
            disconnect.Dispose();
        }
    }
}
