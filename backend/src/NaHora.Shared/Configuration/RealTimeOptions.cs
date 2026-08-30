using System.ComponentModel.DataAnnotations;

namespace NaHora.Shared.Configuration;

/// <summary>
/// Limits for the server-sent events channel.
/// </summary>
/// <remarks>
/// In Shared rather than alongside the other options because both sides read
/// it: the registry sizes its buffer from it, and the endpoint paces its
/// keep-alive from it. A slice may not reference Infrastructure, not even for a
/// settings type, so the alternative would be the same section bound twice.
/// </remarks>
public sealed class RealTimeOptions
{
    public const string SectionName = "RealTime";

    /// <summary>
    /// Events held for an observer that is not keeping up.
    /// </summary>
    /// <remarks>
    /// Small on purpose. The buffer exists to absorb a burst, not to preserve
    /// history: a dashboard that fell fifty events behind wants the current
    /// state, and the oldest entries are the ones it needs least.
    /// </remarks>
    [Range(1, 1_000)]
    public int ObserverBufferSize { get; init; } = 32;

    /// <summary>
    /// Silence after which a keep-alive is emitted.
    /// </summary>
    /// <remarks>
    /// Under the idle timeout of the proxies typically sitting in front of the
    /// API, which is where an otherwise healthy connection gets closed.
    /// </remarks>
    [Range(5, 300)]
    public int HeartbeatSeconds { get; init; } = 20;

    /// <summary>
    /// Reconnection delay suggested to the client, in milliseconds.
    /// </summary>
    /// <remarks>
    /// Sent on the stream so a mass disconnect does not turn into every client
    /// reconnecting at the same instant.
    /// </remarks>
    [Range(500, 60_000)]
    public int ReconnectionMilliseconds { get; init; } = 3_000;
}
