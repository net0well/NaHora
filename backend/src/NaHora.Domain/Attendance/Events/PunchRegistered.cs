using NaHora.Domain.Abstractions;

namespace NaHora.Domain.Attendance.Events;

/// <summary>A punch was recorded.</summary>
/// <remarks>
/// Carries no coordinate. Consumers that need the location read the punch; a
/// message that travels through the broker and the outbox table should not put
/// someone's whereabouts in two more places.
/// </remarks>
public sealed record PunchRegistered(
    Guid CompanyId,
    Guid EmployeeId,
    Guid PunchId,
    long Nsr,
    string Direction,
    string Trust,
    DateTimeOffset OccurredAt) : IAuditableEvent;
