namespace NaHora.Features.Attendance.RegisterPunch;

/// <summary>
/// Confirmation of a recorded punch.
/// </summary>
/// <remarks>
/// Carries the official time so the app can show what was actually recorded
/// rather than what the phone believed. When the two differ, the employee
/// should see the one that counts.
/// </remarks>
public sealed record RegisterPunchResponse(
    Guid Id,
    long Nsr,
    string Direction,
    string Origin,
    string Trust,
    DateTimeOffset OccurredAt,
    DateOnly LocalDate,
    bool AlreadyRegistered);
