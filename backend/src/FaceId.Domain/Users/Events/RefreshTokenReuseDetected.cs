using FaceId.Domain.Abstractions;

namespace FaceId.Domain.Users.Events;

/// <summary>
/// A refresh token that had already been rotated was presented again.
/// </summary>
/// <remarks>
/// The strongest signal of a stolen credential this system produces. The
/// legitimate holder and the thief cannot be told apart, so the whole family is
/// revoked and this event records that it happened.
/// </remarks>
public sealed record RefreshTokenReuseDetected(
    Guid CompanyId,
    Guid UserId,
    Guid FamilyId,
    int TokensRevoked,
    DateTimeOffset OccurredAt) : IAuditableEvent;
