using FaceId.Domain.Abstractions;

namespace FaceId.Domain.Users.Events;

/// <summary>A sign-in attempt was rejected.</summary>
/// <remarks>
/// <paramref name="UserId"/> is null when the CPF matched no account. That
/// distinction is recorded here, where only the audit trail sees it — the HTTP
/// response deliberately gives it away to nobody.
/// </remarks>
public sealed record SignInFailed(
    Guid CompanyId,
    Guid? UserId,
    string? IpAddress,
    DateTimeOffset OccurredAt) : IAuditableEvent;
