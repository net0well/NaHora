namespace NaHora.Domain.RealTime;

/// <summary>
/// Registers an observer and yields the events addressed to its company.
/// </summary>
/// <remarks>
/// Separate from <see cref="IRealTimeBroadcaster"/> because the two sides have
/// no caller in common: the endpoint only ever observes, and the relay handler
/// only ever broadcasts. One interface would hand each of them a method it must
/// never call.
/// </remarks>
public interface IRealTimeObserverRegistry
{
    /// <summary>
    /// Observes until the caller stops enumerating or the token is cancelled,
    /// at which point the registration is released.
    /// </summary>
    IAsyncEnumerable<RealTimeEvent> ObserveAsync(Guid companyId, CancellationToken cancellationToken);
}
