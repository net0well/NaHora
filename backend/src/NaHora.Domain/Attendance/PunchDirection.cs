namespace NaHora.Domain.Attendance;

/// <summary>Which boundary of the working day a punch marks.</summary>
public enum PunchDirection
{
    Entry = 1,
    BreakStart = 2,
    BreakEnd = 3,
    Exit = 4
}
