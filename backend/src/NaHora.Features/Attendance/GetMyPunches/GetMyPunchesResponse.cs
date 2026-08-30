namespace NaHora.Features.Attendance.GetMyPunches;

/// <summary>
/// A working day and what was recorded in it.
/// </summary>
/// <param name="LocalDate">The day these punches belong to, in company time.</param>
/// <param name="TimeZoneId">The company timezone, so the app renders the day the same way the server decided it.</param>
/// <param name="NextDirection">
/// Which boundary the next punch would mark. Advisory only: the server decides
/// again when the punch actually arrives, and a stale screen must not be able
/// to change what gets recorded.
/// </param>
public sealed record GetMyPunchesResponse(
    DateOnly LocalDate,
    string TimeZoneId,
    string NextDirection,
    IReadOnlyList<DayPunchResponse> Punches);
