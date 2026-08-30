using FaceId.Domain.Abstractions;

namespace FaceId.Domain.Users.Events;

/// <summary>An account was locked by repeated failures.</summary>
public sealed record AccountLocked(
    Guid CompanyId,
    Guid UserId,
    int FailedAttempts,
    DateTimeOffset LockedUntil,
    DateTimeOffset OccurredAt) : IAuditableEvent;
