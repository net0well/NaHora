using NaHora.Domain.Abstractions;

namespace NaHora.Domain.Users.Events;

/// <summary>A sign-in succeeded.</summary>
/// <remarks>
/// Carries identifiers, never credentials. The CPF is deliberately absent: the
/// event travels through a broker and lands in a consumer's log, and neither is
/// a place for a document number.
/// </remarks>
public sealed record UserSignedIn(
    Guid CompanyId,
    Guid UserId,
    string Role,
    DateTimeOffset OccurredAt) : IAuditableEvent;
