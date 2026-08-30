namespace NaHora.Domain.Attendance;

/// <summary>
/// Which boundary of the working day a punch marks, from the ones already
/// recorded that day.
/// </summary>
/// <remarks>
/// Lives in the domain because two places ask it: the handler that records a
/// punch, and the screen that tells the employee what the next one will be. Two
/// copies would eventually disagree, and the disagreement shows up as the app
/// promising "Entrada" while the server writes "Saída".
///
/// The server decides. Letting the client choose would allow two entries in a
/// row, and the correction lands on a manager.
/// </remarks>
public static class PunchDirectionSequence
{
    /// <summary>How many punches make one full cycle of the day.</summary>
    private const int CycleLength = 4;

    /// <summary>
    /// The boundary the next punch marks, given how many are already recorded
    /// for that day.
    /// </summary>
    /// <remarks>
    /// Entry, break out, break in, exit — then round again for a split shift,
    /// which is common enough in Brazil to be the ordinary case rather than an
    /// edge one.
    /// </remarks>
    public static PunchDirection NextAfter(int punchesRecordedToday) =>
        (punchesRecordedToday % CycleLength) switch
        {
            0 => PunchDirection.Entry,
            1 => PunchDirection.BreakStart,
            2 => PunchDirection.BreakEnd,
            _ => PunchDirection.Exit
        };
}
