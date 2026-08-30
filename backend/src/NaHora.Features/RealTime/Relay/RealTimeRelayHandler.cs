using NaHora.Domain.RealTime;

namespace NaHora.Features.RealTime.Relay;

/// <summary>
/// Hands an event that arrived from the broker to the observers connected here.
/// </summary>
/// <remarks>
/// The last step of the fan-out. Every instance runs this for every event,
/// because every instance holds a different set of connections — which is why
/// its queue is private rather than shared.
///
/// It does nothing when nobody is connected, and that is the normal case, not a
/// failure: outside business hours no dashboard is open.
/// </remarks>
public static class RealTimeRelayHandler
{
    public static ValueTask Handle(
        RealTimeEvent notification,
        IRealTimeBroadcaster broadcaster,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(broadcaster);

        return broadcaster.BroadcastAsync(notification, cancellationToken);
    }
}
