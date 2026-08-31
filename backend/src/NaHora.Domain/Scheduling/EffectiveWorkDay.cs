namespace NaHora.Domain.Scheduling;

/// <summary>
/// Decides which schedule governs a given day: the employee's own, or the
/// company's.
/// </summary>
/// <remarks>
/// A pure function with no I/O, so the rule can be tested without a database
/// and reused by every caller that needs it — the punch screen, the day list
/// and the handler that classifies a punch against what was expected.
/// </remarks>
public static class EffectiveWorkDay
{
    /// <summary>
    /// The employee's version when one covers the date, otherwise the company
    /// default, otherwise a day off.
    /// </summary>
    /// <remarks>
    /// Falling back to a day off rather than to some assumed nine-to-five: a
    /// company that has not set its hours has not told us anything, and
    /// inventing hours would produce lateness nobody agreed to.
    /// </remarks>
    public static WorkDaySchedule On(
        DateOnly date,
        IEnumerable<EmployeeWorkSchedule>? employeeSchedules,
        CompanyWorkSchedule? companySchedule)
    {
        var own = employeeSchedules?.FirstOrDefault(schedule => schedule.AppliesOn(date));

        if (own is not null)
        {
            return own.For(date.DayOfWeek);
        }

        return companySchedule?.For(date.DayOfWeek) ?? WorkDaySchedule.Off(date.DayOfWeek);
    }
}
