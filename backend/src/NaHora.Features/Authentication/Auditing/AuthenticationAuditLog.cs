using Microsoft.Extensions.Logging;

namespace NaHora.Features.Authentication.Auditing;

/// <summary>
/// Source-generated log messages for the authentication audit trail.
/// </summary>
/// <remarks>
/// Generated at compile time rather than formatted at call time: no boxing of
/// the identifiers, no allocation when the level is disabled, and the event ids
/// stay stable so a log query can key on them instead of on message text.
/// </remarks>
internal static partial class AuthenticationAuditLog
{
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Sign-in succeeded for user {UserId} of company {CompanyId} with role {Role}")]
    public static partial void SignInSucceeded(ILogger logger, Guid userId, Guid companyId, string role);

    // Warning rather than Information: one failure is noise, but the rate of
    // these is what reveals a spraying attempt, and that only surfaces if the
    // level makes it visible.
    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Warning,
        Message = "Sign-in failed for company {CompanyId}, user {UserId}, from {IpAddress}")]
    public static partial void SignInFailed(ILogger logger, Guid companyId, Guid? userId, string? ipAddress);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Warning,
        Message = "Account {UserId} of company {CompanyId} locked until {LockedUntil} after {FailedAttempts} attempt(s)")]
    public static partial void AccountLocked(
        ILogger logger,
        Guid userId,
        Guid companyId,
        DateTimeOffset lockedUntil,
        int failedAttempts);

    // Error, not warning. This is the strongest indication of a stolen
    // credential the system produces, and it should reach whoever watches
    // errors without anyone going looking for it.
    [LoggerMessage(
        EventId = 1004,
        Level = LogLevel.Error,
        Message = "Refresh token reuse detected for user {UserId} of company {CompanyId}; "
                  + "family {FamilyId} revoked, {TokensRevoked} token(s) invalidated")]
    public static partial void RefreshTokenReuseDetected(
        ILogger logger,
        Guid userId,
        Guid companyId,
        Guid familyId,
        int tokensRevoked);
}
