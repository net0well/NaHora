namespace FaceId.Domain.Employees;

/// <summary>
/// State of the employment relationship.
/// </summary>
/// <remarks>
/// None of these values blocks a punch on its own. Ordinance 671/2021, article
/// 78 §1, forbids a time clock from restricting the record, so being on leave
/// flags the punch for review rather than refusing it.
/// </remarks>
public enum EmployeeStatus
{
    Active = 0,

    /// <summary>On leave. May still punch; the record is flagged for review.</summary>
    OnLeave = 1,

    /// <summary>
    /// Terminated. Terminal: rehiring creates a new employment record so the
    /// periods stay separate in the regulatory export.
    /// </summary>
    Terminated = 2
}
