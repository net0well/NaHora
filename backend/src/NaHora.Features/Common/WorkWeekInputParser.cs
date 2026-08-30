using System.Globalization;
using NaHora.Domain.Errors;
using NaHora.Domain.Scheduling;
using NaHora.Shared.Results;

namespace NaHora.Features.Common;

/// <summary>
/// Turns the seven days a form posts into the domain's value objects.
/// </summary>
/// <remarks>
/// Shared by the company and the employee slices because both accept the same
/// week, and slices do not reference each other. Collects every problem before
/// returning: a week rejected one field at a time is a week nobody finishes
/// editing.
/// </remarks>
public static class WorkWeekInputParser
{
    /// <summary>The only shape accepted, and the one the API hands back.</summary>
    private const string TimeFormat = "HH:mm";

    public static Result<IReadOnlyList<WorkDaySchedule>> Parse(IReadOnlyList<WorkDayInput>? days)
    {
        if (days is null || days.Count == 0)
        {
            return Result.Failure<IReadOnlyList<WorkDaySchedule>>(ScheduleErrors.WeekIncomplete());
        }

        var parsed = new List<WorkDaySchedule>(days.Count);
        var errors = new List<Error>();

        foreach (var day in days)
        {
            if (!Enum.IsDefined((DayOfWeek)day.DayOfWeek))
            {
                errors.Add(ScheduleErrors.WeekIncomplete());

                continue;
            }

            var dayOfWeek = (DayOfWeek)day.DayOfWeek;

            var times = new[]
            {
                ParseTime(day.StartTime, dayOfWeek),
                ParseTime(day.LunchStartTime, dayOfWeek),
                ParseTime(day.LunchEndTime, dayOfWeek),
                ParseTime(day.EndTime, dayOfWeek)
            };

            if (times.Any(time => time.IsFailure))
            {
                errors.AddRange(times.Where(time => time.IsFailure).SelectMany(time => time.Errors));

                continue;
            }

            var schedule = WorkDaySchedule.Create(
                dayOfWeek,
                times[0].Value,
                times[1].Value,
                times[2].Value,
                times[3].Value);

            if (schedule.IsFailure)
            {
                errors.AddRange(schedule.Errors);
            }
            else
            {
                parsed.Add(schedule.Value);
            }
        }

        if (errors.Count > 0)
        {
            return Result.Failure<IReadOnlyList<WorkDaySchedule>>(errors);
        }

        return WorkWeek.Complete(parsed);
    }

    /// <summary>
    /// Parses "08:00". Blank means the field was left empty, which is a
    /// legitimate answer — a day off, or a day with no break.
    /// </summary>
    private static Result<TimeOnly?> ParseTime(string? value, DayOfWeek day)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Success<TimeOnly?>(null);
        }

        return TimeOnly.TryParseExact(value.Trim(), TimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var time)
            ? Result.Success<TimeOnly?>(time)
            : Result.Failure<TimeOnly?>(ScheduleErrors.TimeMalformed(day, value));
    }
}
