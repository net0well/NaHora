using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FaceId.Domain.RealTime;
using FaceId.Domain.Users;
using FaceId.Infrastructure.Persistence;
using FaceId.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace FaceId.IntegrationTests.RealTime;

/// <summary>
/// The live event stream, over the real HTTP pipeline.
/// </summary>
public sealed class EventStreamTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private readonly HttpClient _client = fixture.CreateClient();

    [Fact]
    public async Task Stream_WithoutAToken_ReturnsUnauthorized()
    {
        // Arrange & Act
        var response = await _client.GetAsync(
            new Uri("/api/v1/events/stream", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Stream_WhenOpened_AnnouncesItselfBeforeAnyEventExists()
    {
        // Arrange
        var account = await SeedAsync();

        // Act
        using var response = await OpenStreamAsync(await SignInAsync(account));
        using var reader = await ReadStreamAsync(response);

        // Assert — headers first, so a client knows the subscription is live
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("text/event-stream");

        var opening = await ReadEventAsync(reader);

        // The immediate keep-alive: without it the client would sit on a silent
        // socket, unable to tell a working subscription from a broken one.
        opening.Type.ShouldBe(RealTimeEventType.Heartbeat);
        opening.Retry.ShouldNotBeNull("the client must be told when to come back");
    }

    [Fact]
    public async Task Stream_CarriesTheCallersCompanyAndNoOther()
    {
        // Arrange
        var account = await SeedAsync();
        var somebodyElse = Guid.CreateVersion7();

        using var response = await OpenStreamAsync(await SignInAsync(account));
        using var reader = await ReadStreamAsync(response);

        (await ReadEventAsync(reader)).Type.ShouldBe(RealTimeEventType.Heartbeat);

        var broadcaster = fixture.Services.GetRequiredService<IRealTimeBroadcaster>();

        // Act — the other company's event goes out first, so if isolation
        // leaked it would be the one waiting to be read
        await broadcaster.BroadcastAsync(
            RealTimeEvent.Create(RealTimeEventType.AccountLocked, somebodyElse, DateTimeOffset.UtcNow),
            TestContext.Current.CancellationToken);

        await broadcaster.BroadcastAsync(
            RealTimeEvent.Create(RealTimeEventType.AccountLocked, account.CompanyId, DateTimeOffset.UtcNow),
            TestContext.Current.CancellationToken);

        // Assert
        var delivered = await ReadEventAsync(reader);

        delivered.Type.ShouldBe(RealTimeEventType.AccountLocked);
        JsonDocument.Parse(delivered.Data).RootElement
            .GetProperty("companyId").GetGuid()
            .ShouldBe(account.CompanyId, "the stream must never carry another company's events");
    }

    private async Task<HttpResponseMessage> OpenStreamAsync(string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/api/v1/events/stream", UriKind.Relative));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        // Headers only: waiting for the body to finish would wait forever, which
        // is the point of the endpoint.
        return await _client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            TestContext.Current.CancellationToken);
    }

    private static async Task<StreamReader> ReadStreamAsync(HttpResponseMessage response) =>
        new(await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken));

    /// <summary>
    /// Reads one server-sent event: the field lines up to the blank line that
    /// terminates it.
    /// </summary>
    /// <remarks>
    /// Bounded by its own deadline. Without one a regression here would hang the
    /// suite rather than fail it, and a hung run tells nobody what broke.
    /// </remarks>
    private static async Task<ServerSentEvent> ReadEventAsync(StreamReader reader)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(15));

        string? type = null;
        string? data = null;
        string? retry = null;

        while (await reader.ReadLineAsync(deadline.Token) is { } line)
        {
            if (line.Length == 0)
            {
                return new ServerSentEvent(
                    type ?? throw new InvalidOperationException("event arrived without a type"),
                    data ?? string.Empty,
                    retry);
            }

            var separator = line.IndexOf(':', StringComparison.Ordinal);
            if (separator < 0)
            {
                continue;
            }

            var value = line[(separator + 1)..].TrimStart();

            switch (line[..separator])
            {
                case "event": type = value; break;
                case "data": data = value; break;
                case "retry": retry = value; break;
                default: break;
            }
        }

        throw new InvalidOperationException("the stream closed before an event was delivered");
    }

    private async Task<SeededAccount> SeedAsync() =>
        await fixture.WithScopeAsync(async services =>
        {
            var dbContext = services.GetRequiredService<FaceIdDbContext>();
            var hasher = services.GetRequiredService<IPasswordHasher>();

            return await AuthenticationSeed.CreateAsync(dbContext, hasher, DateTimeOffset.UtcNow);
        });

    private async Task<string> SignInAsync(SeededAccount account)
    {
        var response = await _client.PostAsJsonAsync(
            new Uri("/api/v1/auth/login", UriKind.Relative),
            new { cpf = account.Cpf, password = AuthenticationSeed.Password, companyId = account.CompanyId },
            TestContext.Current.CancellationToken);

        var body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        return body.RootElement.GetProperty("accessToken").GetString()!;
    }

    private sealed record ServerSentEvent(string Type, string Data, string? Retry);
}
