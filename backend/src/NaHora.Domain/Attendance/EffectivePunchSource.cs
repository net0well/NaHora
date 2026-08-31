namespace NaHora.Domain.Attendance;

/// <summary>
/// Where the time that counts came from.
/// </summary>
/// <remarks>
/// The employee is entitled to know which of their marks were touched. A screen
/// that showed a corrected time exactly like a captured one would be hiding a
/// manager's edit inside the employee's own record.
/// </remarks>
public enum EffectivePunchSource
{
    /// <summary>Captured by the registrar and never corrected.</summary>
    Recorded = 0,

    /// <summary>Captured, then given a different time by an approved treatment.</summary>
    Adjusted = 1,

    /// <summary>Never captured. It counts because a treatment was approved.</summary>
    Included = 2
}
