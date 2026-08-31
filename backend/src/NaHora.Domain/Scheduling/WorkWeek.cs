using NaHora.Domain.Errors;
using NaHora.Shared.Results;

namespace NaHora.Domain.Scheduling;

/// <summary>
/// The rules a week of <see cref="WorkDaySchedule"/> has to satisfy, shared by
/// the company default and the per-employee override.
/// </summary>
/// <remarks>
/// A static helper rather than a type of its own: the days are persisted as an
/// owned collection on each aggregate, and wrapping them in another object
/// would buy nothing the two aggregates do not already give.
/// </remarks>
public static class WorkWeek
{
    public const int DayCount = 7;

    /// <summary>
    /// Accepts a week only when all seven days are present exactly once.
    /// </summary>
    /// <remarks>
    /// A missing day is not the same as a day off, and treating it as one would
    /// silently turn a partial edit into an unpaid Saturday.
    /// </remarks>
    public static Result<IReadOnlyList<WorkDaySchedule>> Complete(IEnumerable<WorkDaySchedule>? days)
    {
        var ordered = days?.OrderBy(day => day.Day).ToList() ?? [];

        var covered = ordered.Select(day => day.Day).Distinct().Count();
        if (ordered.Count != DayCount || covered != DayCount)
        {
            return Result.Failure<IReadOnlyList<WorkDaySchedule>>(ScheduleErrors.WeekIncomplete());
        }

        return Result.Success<IReadOnlyList<WorkDaySchedule>>(ordered);
    }

    /// <summary>
    /// Monday to Friday with the given hours, weekend off.
    /// </summary>
    /// <remarks>
    /// The shape almost every Brazilian office starts from. It exists so a new
    /// company has a schedule from its first day rather than a blank the
    /// employee's screen has to apologise for.
    /// </remarks>
    public static IReadOnlyList<WorkDaySchedule> MondayToFriday(
        TimeOnly startTime,
        TimeOnly? lunchStartTime,
        TimeOnly? lunchEndTime,
        TimeOnly endTime)
    {
        var week = new List<WorkDaySchedule>(DayCount);

        foreach (var day in Enum.GetValues<DayOfWeek>().OrderBy(day => day))
        {
            var isWeekend = day is DayOfWeek.Saturday or DayOfWeek.Sunday;

            week.Add(isWeekend
                ? WorkDaySchedule.Off(day)
                : WorkDaySchedule.Create(day, startTime, lunchStartTime, lunchEndTime, endTime).Value);
        }

        return week;
    }
}
