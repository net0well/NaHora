using Microsoft.Extensions.Logging;
using NaHora.Domain.Users.Events;

namespace NaHora.Features.Authentication.Auditing;

/// <summary>
/// Records authentication events as a structured audit trail.
/// </summary>
/// <remarks>
/// Deliberately the simplest useful consumer. It exists now because the
/// authentication events are worth recording from day one, and it becomes the
/// place where SecurityEvent is created in a later phase — the messaging
/// infrastructure around it will not have to change.
///
/// Wolverine discovers each method by its message parameter. They run through
/// the outbox, so an event only arrives here if the transaction that produced
/// it committed.
/// </remarks>
public static class AuthenticationAuditHandler
{
    public static void Handle(UserSignedIn message, ILogger<AuthenticationAuditHandlerCategory> logger)
    {
        ArgumentNullException.ThrowIfNull(message);

        AuthenticationAuditLog.SignInSucceeded(logger, message.UserId, message.CompanyId, message.Role);
    }

    public static void Handle(SignInFailed message, ILogger<AuthenticationAuditHandlerCategory> logger)
    {
        ArgumentNullException.ThrowIfNull(message);

        AuthenticationAuditLog.SignInFailed(logger, message.CompanyId, message.UserId, message.IpAddress);
    }

    public static void Handle(AccountLocked message, ILogger<AuthenticationAuditHandlerCategory> logger)
    {
        ArgumentNullException.ThrowIfNull(message);

        AuthenticationAuditLog.AccountLocked(
            logger,
            message.UserId,
            message.CompanyId,
            message.LockedUntil,
            message.FailedAttempts);
    }

    public static void Handle(RefreshTokenReuseDetected message, ILogger<AuthenticationAuditHandlerCategory> logger)
    {
        ArgumentNullException.ThrowIfNull(message);

        AuthenticationAuditLog.RefreshTokenReuseDetected(
            logger,
            message.UserId,
            message.CompanyId,
            message.FamilyId,
            message.TokensRevoked);
    }
}
