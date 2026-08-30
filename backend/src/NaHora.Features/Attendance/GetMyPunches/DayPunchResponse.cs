namespace NaHora.Features.Attendance.GetMyPunches;

/// <summary>
/// One punch, as the day's list shows it.
/// </summary>
/// <remarks>
/// Carries no coordinates. The app already holds the location of a punch it
/// just made, and shipping every past position back to the device would put a
/// map of where the person has been on the least protected part of the system.
/// Accuracy is enough to explain why a record is marked degraded.
/// </remarks>
/// <param name="UtcOffsetSeconds">
/// The company offset in force when this punch happened, so the app renders it
/// in company hours without needing a separately synchronised clock — and
/// without a punch made before a daylight-saving change shifting by an hour.
/// </param>
public sealed record DayPunchResponse(
    Guid Id,
    long Nsr,
    string Direction,
    string Origin,
    string Trust,
    DateTimeOffset OccurredAt,
    int UtcOffsetSeconds,
    double AccuracyMeters);
