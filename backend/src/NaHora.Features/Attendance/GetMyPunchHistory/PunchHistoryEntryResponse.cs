namespace NaHora.Features.Attendance.GetMyPunchHistory;

/// <summary>
/// One past mark, as the history list shows it, after approved treatments.
/// </summary>
/// <remarks>
/// Carries the local date so the app can group by working day without
/// recomputing it from a timestamp — the day was decided on the server, in
/// company time, and recomputing it on a device in another timezone would split
/// one day into two.
/// </remarks>
/// <param name="Nsr">
/// Null for an inclusion. Nothing was captured, so no registrar numbered it.
/// </param>
/// <param name="Source">
/// <c>Recorded</c>, <c>Adjusted</c> or <c>Included</c>. The employee is
/// entitled to see which of their marks were touched and which were granted.
/// </param>
/// <param name="OriginalOccurredAt">
/// The time a correction replaced, kept beside the one in force. Null when
/// nothing was replaced.
/// </param>
/// <param name="UtcOffsetSeconds">
/// The company offset in force when this mark happened. Sent per entry rather
/// than per page so a punch made before a daylight-saving change still renders
/// at the hour it was recorded at.
/// </param>
public sealed record PunchHistoryEntryResponse(
    Guid Id,
    long? Nsr,
    string Direction,
    string? Origin,
    string? Trust,
    string Source,
    DateTimeOffset OccurredAt,
    DateTimeOffset? OriginalOccurredAt,
    DateOnly LocalDate,
    int UtcOffsetSeconds,
    double? AccuracyMeters);
