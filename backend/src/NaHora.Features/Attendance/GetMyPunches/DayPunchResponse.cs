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
public sealed record DayPunchResponse(
    Guid Id,
    long Nsr,
    string Direction,
    string Origin,
    string Trust,
    DateTimeOffset OccurredAt,
    double AccuracyMeters);
