using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using FaceId.Domain.RealTime;
using FaceId.Infrastructure.Configuration;
using FaceId.Shared.Configuration;
using Microsoft.Extensions.Options;

namespace FaceId.Infrastructure.RealTime;

/// <summary>
/// Holds the observers connected to this instance, grouped by company.
/// </summary>
/// <remarks>
/// In memory and per instance, which is the whole design: a restart drops the
/// connections and the clients reconnect, and reaching observers elsewhere is
/// the broker's job. There is no shared state to keep consistent.
///
/// Public because Wolverine's generated code constructs handler dependencies
/// directly rather than resolving them from the container.
/// </remarks>
public sealed class InMemoryRealTimeObserverRegistry(IOptions<RealTimeOptions> options)
    : IRealTimeBroadcaster, IRealTimeObserverRegistry
{
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, Channel<RealTimeEvent>>> observersByCompany = new();

    public async IAsyncEnumerable<RealTimeEvent> ObserveAsync(
        Guid companyId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var channel = Channel.CreateBounded<RealTimeEvent>(
            new BoundedChannelOptions(options.Value.ObserverBufferSize)
            {
                // A slow observer drops its oldest events and keeps running.
                // Blocking the broadcaster instead would let one stalled
                // dashboard hold up delivery to everyone else in the company.
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false
            });

        var observerId = Guid.CreateVersion7();
        var observers = observersByCompany.GetOrAdd(companyId, _ => new ConcurrentDictionary<Guid, Channel<RealTimeEvent>>());
        observers[observerId] = channel;

        try
        {
            await foreach (var notification in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return notification;
            }
        }
        finally
        {
            observers.TryRemove(observerId, out _);
        }

        // The company's (now empty) bucket is left in place. Removing it would
        // race with a connection arriving for the same company, and the cost of
        // keeping it is one empty dictionary per company that has ever
        // connected — bounded by the customer list, not by traffic.
    }

    public ValueTask BroadcastAsync(RealTimeEvent notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        if (observersByCompany.TryGetValue(notification.CompanyId, out var observers))
        {
            foreach (var channel in observers.Values)
            {
                // TryWrite rather than WriteAsync: the bound is enforced by
                // dropping, so a write never has to wait, and broadcasting must
                // not be able to block the caller that produced the event.
                channel.Writer.TryWrite(notification);
            }
        }

        return ValueTask.CompletedTask;
    }
}
