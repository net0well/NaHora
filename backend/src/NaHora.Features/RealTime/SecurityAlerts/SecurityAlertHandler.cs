using System.Globalization;
using NaHora.Domain.RealTime;
using NaHora.Domain.Users.Events;

namespace NaHora.Features.RealTime.SecurityAlerts;

/// <summary>
/// Turns the security events worth interrupting someone for into live updates.
/// </summary>
/// <remarks>
/// Deliberately selective. A failed sign-in is noise — people mistype passwords
/// all day, and a dashboard that reports each one gets ignored, taking the
/// events that matter with it. What reaches the screen is the point at which
/// something stopped looking like a mistake: an account locked out, or a
/// refresh token replayed, which is a token in two places at once.
///
/// The returned event is a cascading message: Wolverine publishes it to the
/// real-time exchange, which fans it out to every instance, each of which
/// delivers it to its own observers.
/// </remarks>
public static class SecurityAlertHandler
{
    public static RealTimeEvent Handle(AccountLocked message)
    {
        ArgumentNullException.ThrowIfNull(message);

        return RealTimeEvent.Create(
            RealTimeEventType.AccountLocked,
            message.CompanyId,
            message.OccurredAt,
            new Dictionary<string, string>
            {
                ["userId"] = message.UserId.ToString(),
                ["failedAttempts"] = message.FailedAttempts.ToString(CultureInfo.InvariantCulture),
                ["lockedUntil"] = message.LockedUntil.ToString("O", CultureInfo.InvariantCulture)
            });
    }

    public static RealTimeEvent Handle(RefreshTokenReuseDetected message)
    {
        ArgumentNullException.ThrowIfNull(message);

        return RealTimeEvent.Create(
            RealTimeEventType.RefreshTokenReuseDetected,
            message.CompanyId,
            message.OccurredAt,
            new Dictionary<string, string>
            {
                ["userId"] = message.UserId.ToString(),
                ["tokensRevoked"] = message.TokensRevoked.ToString(CultureInfo.InvariantCulture)
            });
    }
}
