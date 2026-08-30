namespace NaHora.Domain.RealTime;

/// <summary>
/// Names of the events carried on the real-time channel.
/// </summary>
/// <remarks>
/// The client switches on these to decide what to render, which makes them part
/// of the public contract: renaming one is a breaking change, not a refactor.
/// </remarks>
public static class RealTimeEventType
{
    /// <summary>An account was locked after repeated failed sign-ins.</summary>
    public const string AccountLocked = "security.account_locked";

    /// <summary>A refresh token was replayed, and its whole family was revoked.</summary>
    public const string RefreshTokenReuseDetected = "security.refresh_token_reuse_detected";

    /// <summary>
    /// Emitted on a timer to keep the connection warm.
    /// </summary>
    /// <remarks>
    /// An idle SSE connection is indistinguishable from a dead one to an
    /// intermediary proxy, which closes it. Traffic is what keeps it open.
    /// </remarks>
    public const string Heartbeat = "heartbeat";
}
