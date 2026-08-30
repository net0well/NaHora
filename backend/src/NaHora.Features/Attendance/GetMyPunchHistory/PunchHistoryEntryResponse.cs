namespace NaHora.Features.Attendance.GetMyPunchHistory;

/// <summary>
/// One past punch, as the history list shows it.
/// </summary>
/// <remarks>
/// Carries the local date so the app can group by working day without
/// recomputing it from a timestamp — the day was decided on the server, in
/// company time, and recomputing it on a device in another timezone would split
/// one day into two.
/// </remarks>
/// <param name="UtcOffsetSeconds">
/// The company offset in force when this punch happened. Sent per entry rather
/// than per page so a punch made before a daylight-saving change still renders
/// at the hour it was recorded at.
/// </param>
public sealed record PunchHistoryEntryResponse(
    Guid Id,
    long Nsr,
    string Direction,
    string Origin,
    string Trust,
    DateTimeOffset OccurredAt,
    DateOnly LocalDate,
    int UtcOffsetSeconds,
    double AccuracyMeters);
