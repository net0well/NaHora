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
/// <param name="ExpectedPunches">
/// What a complete day looks like under the schedule in force — four with a
/// break, two without, none on a day off.
/// </param>
/// <param name="IsBeyondSchedule">
/// True when more has been recorded than the day expected. Advisory only: the
/// next punch is still recorded and still gets an NSR, because Art. 78 §1º
/// forbids the registrar from restricting the record. What this buys is a
/// warning before the fact and a flag the manager can act on after it.
/// </param>
public sealed record GetMyPunchesResponse(
    DateOnly LocalDate,
    string TimeZoneId,
    string NextDirection,
    int ExpectedPunches,
    bool IsBeyondSchedule,
    IReadOnlyList<DayPunchResponse> Punches);
