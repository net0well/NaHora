namespace NaHora.Features.Attendance.GetMyPunches;

/// <summary>
/// One mark, as the day's list shows it, after approved treatments.
/// </summary>
/// <remarks>
/// Carries no coordinates. The app already holds the location of a punch it
/// just made, and shipping every past position back to the device would put a
/// map of where the person has been on the least protected part of the system.
/// Accuracy is enough to explain why a record is marked degraded.
/// </remarks>
/// <param name="Nsr">
/// Null for an inclusion. Nothing was captured, so no registrar numbered it —
/// sending a zero would put a number an inspection could cite on a row that
/// does not have one.
/// </param>
/// <param name="Source">
/// <c>Recorded</c>, <c>Adjusted</c> or <c>Included</c>. The employee is
/// entitled to know which of their marks were touched, and a corrected time
/// drawn exactly like a captured one hides a decision inside their own record.
/// </param>
/// <param name="OriginalOccurredAt">
/// The time a correction replaced, kept beside the one in force. Null when
/// nothing was replaced.
/// </param>
/// <param name="UtcOffsetSeconds">
/// The company offset in force when this mark happened, so the app renders it
/// in company hours without needing a separately synchronised clock — and
/// without a punch made before a daylight-saving change shifting by an hour.
/// </param>
public sealed record DayPunchResponse(
    Guid Id,
    long? Nsr,
    string Direction,
    string? Origin,
    string? Trust,
    string Source,
    DateTimeOffset OccurredAt,
    DateTimeOffset? OriginalOccurredAt,
    int UtcOffsetSeconds,
    double? AccuracyMeters);
