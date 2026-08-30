using System.Globalization;
using NaHora.Domain.Scheduling;

namespace NaHora.Features.Common;

/// <summary>
/// One day of a working week on the wire.
/// </summary>
/// <remarks>
/// Lives in Common because several slices return a week and slices do not
/// reference each other.
///
/// <paramref name="Describe"/> travels alongside the times rather than instead
/// of them: the punch screen shows one line, and whoever edits the schedule
/// needs the fields separately.
/// </remarks>
/// <param name="ExpectedPunches">
/// What a complete day looks like — four with a break, two without, none on a
/// day off. Advisory: the app uses it to say "you have already recorded the
/// day", never to refuse the next punch.
/// </param>
public sealed record WorkDayResponse(
    string DayOfWeek,
    int DayOfWeekNumber,
    bool IsWorkday,
    string? StartTime,
    string? LunchStartTime,
    string? LunchEndTime,
    string? EndTime,
    int ExpectedMinutes,
    int ExpectedPunches,
    string Describe)
{
    public static WorkDayResponse From(WorkDaySchedule day)
    {
        ArgumentNullException.ThrowIfNull(day);

        return new WorkDayResponse(
            day.Day.ToString(),
            (int)day.Day,
            day.IsWorkday,
            Format(day.StartTime),
            Format(day.LunchStartTime),
            Format(day.LunchEndTime),
            Format(day.EndTime),
            day.ExpectedMinutes,
            day.ExpectedPunches,
            day.Describe());
    }

    /// <summary>"08:00" — the shape a form posts back.</summary>
    /// <remarks>
    /// Invariant culture: this is a wire contract, and a machine reading it
    /// must not get a different separator because of where the server runs.
    /// </remarks>
    private static string? Format(TimeOnly? time) =>
        time?.ToString("HH:mm", CultureInfo.InvariantCulture);
}
