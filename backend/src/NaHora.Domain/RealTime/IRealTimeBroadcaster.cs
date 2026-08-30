namespace NaHora.Domain.RealTime;

/// <summary>
/// Delivers an event to the observers connected to this instance.
/// </summary>
/// <remarks>
/// Local by design. Reaching observers on other instances is the broker's job,
/// not this one's — every instance consumes the event and broadcasts to its
/// own, which is why no shared backplane is needed.
/// </remarks>
public interface IRealTimeBroadcaster
{
    ValueTask BroadcastAsync(RealTimeEvent notification, CancellationToken cancellationToken = default);
}
