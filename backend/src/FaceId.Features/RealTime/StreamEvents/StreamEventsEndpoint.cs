using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using FaceId.Domain.Abstractions;
using FaceId.Domain.RealTime;
using FaceId.Features.Abstractions;
using FaceId.Features.Common;
using FaceId.Shared.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace FaceId.Features.RealTime.StreamEvents;

/// <summary>
/// Streams the company's live events to an authenticated observer.
/// </summary>
internal sealed class StreamEventsEndpoint : IEndpointGroup
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/api/v1/events/stream", Handle)
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicies.EventStream)
            .WithName("StreamEvents")
            .WithTags("Tempo real")
            .WithSummary("Assina os eventos ao vivo da empresa do usuário autenticado.")
            .WithDescription(
                "Fluxo Server-Sent Events. A empresa é resolvida a partir do token: " +
                "não há parâmetro para escolhê-la. Um evento 'heartbeat' é emitido " +
                "durante o silêncio para manter a conexão aberta.")
            .Produces<RealTimeEvent>(StatusCodes.Status200OK, contentType: "text/event-stream")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
    }

    private static ServerSentEventsResult<RealTimeEvent> Handle(
        ICurrentUser currentUser,
        IRealTimeObserverRegistry registry,
        IOptions<RealTimeOptions> options,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        // The audience is taken from the token. A company accepted from the
        // query string would let any authenticated user read another company's
        // stream, which is the one failure this endpoint must not have.
        var companyId = currentUser.CompanyId;

        return TypedResults.ServerSentEvents(
            Compose(
                registry.ObserveAsync(companyId, cancellationToken),
                companyId,
                options.Value,
                clock,
                cancellationToken));
    }

    /// <summary>
    /// Merges the company's events with a keep-alive on an idle timer.
    /// </summary>
    private static async IAsyncEnumerable<SseItem<RealTimeEvent>> Compose(
        IAsyncEnumerable<RealTimeEvent> source,
        Guid companyId,
        RealTimeOptions options,
        TimeProvider clock,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var interval = TimeSpan.FromSeconds(options.HeartbeatSeconds);
        var reconnection = TimeSpan.FromMilliseconds(options.ReconnectionMilliseconds);
        var carriesReconnection = true;

        SseItem<RealTimeEvent> Wrap(RealTimeEvent notification)
        {
            var item = new SseItem<RealTimeEvent>(notification, notification.Type)
            {
                // The client sends this back as Last-Event-ID when it
                // reconnects, which is why the identifier is time-ordered.
                EventId = notification.Id,

                // Announced once. Every client falling back to its own default
                // means they all return at the same instant after a deployment,
                // and the reconnect becomes the second outage.
                ReconnectionInterval = carriesReconnection ? reconnection : null
            };

            carriesReconnection = false;

            return item;
        }

        await using var events = source.GetAsyncEnumerator(cancellationToken);

        // Registration happens on the first read, and it has to happen before
        // the client is told the stream is live. Announcing first left a window
        // where an event published right after the greeting reached nobody: the
        // client believed it was subscribed and the registry had never heard of
        // it. Held across iterations too, because a read may be awaited only
        // once and the keep-alive winning a race must not consume the read that
        // lost it.
        var pending = events.MoveNextAsync().AsTask();

        // Now that the subscription exists, say so — along with the
        // reconnection interval, without waiting out the first idle period.
        yield return Wrap(Heartbeat(companyId, clock));

        while (true)
        {
            using var idle = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            if (await Task.WhenAny(pending, Task.Delay(interval, clock, idle.Token)).ConfigureAwait(false) != pending)
            {
                yield return Wrap(Heartbeat(companyId, clock));

                continue;
            }

            // Releases the timer that lost, so a busy stream does not accumulate
            // one pending timer for every event it delivers.
            await idle.CancelAsync().ConfigureAwait(false);

            if (!await pending.ConfigureAwait(false))
            {
                yield break;
            }

            var notification = events.Current;
            pending = events.MoveNextAsync().AsTask();

            yield return Wrap(notification);
        }
    }

    private static RealTimeEvent Heartbeat(Guid companyId, TimeProvider clock) =>
        RealTimeEvent.Create(RealTimeEventType.Heartbeat, companyId, clock.GetUtcNow());
}
