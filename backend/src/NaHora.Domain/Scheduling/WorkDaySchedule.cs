using NaHora.Domain.Errors;
using NaHora.Shared.Results;

namespace NaHora.Domain.Scheduling;

/// <summary>
/// What one day of the week looks like: when work starts, when it breaks for
/// lunch, and when it ends.
/// </summary>
/// <remarks>
/// <para>
/// A value object: it has no identity of its own and means nothing outside the
/// schedule it belongs to. Two days with the same hours are the same day.
/// </para>
/// <para>
/// <b>This never blocks a punch.</b> It says what was expected, so a record can
/// be compared against it and a manager can be told when the two disagree.
/// Portaria 671, Art. 78 §1º forbids the registrar from restricting the record,
/// and an expectation is not a gate.
/// </para>
/// </remarks>
public sealed record WorkDaySchedule
{
    /// <summary>Entry, lunch out, lunch in, exit.</summary>
    private const int PunchesWithLunch = 4;

    /// <summary>Entry and exit.</summary>
    private const int PunchesWithoutLunch = 2;

    private WorkDaySchedule(
        DayOfWeek day,
        TimeOnly? startTime,
        TimeOnly? lunchStartTime,
        TimeOnly? lunchEndTime,
        TimeOnly? endTime)
    {
        Day = day;
        StartTime = startTime;
        LunchStartTime = lunchStartTime;
        LunchEndTime = lunchEndTime;
        EndTime = endTime;
    }

    public DayOfWeek Day { get; }

    /// <summary>Null means the day has no expected work at all.</summary>
    public TimeOnly? StartTime { get; }

    public TimeOnly? LunchStartTime { get; }

    public TimeOnly? LunchEndTime { get; }

    public TimeOnly? EndTime { get; }

    public bool IsWorkday => StartTime is not null && EndTime is not null;

    public bool HasLunch => LunchStartTime is not null && LunchEndTime is not null;

    /// <summary>
    /// How many punches a complete day produces.
    /// </summary>
    /// <remarks>
    /// The number the app compares against before offering to punch again. It
    /// is what "you have already recorded the day" means — not a limit, and
    /// never a reason to refuse the next one.
    /// </remarks>
    public int ExpectedPunches => !IsWorkday
        ? 0
        : HasLunch ? PunchesWithLunch : PunchesWithoutLunch;

    /// <summary>Working minutes expected, with the break already deducted.</summary>
    public int ExpectedMinutes
    {
        get
        {
            if (!IsWorkday)
            {
                return 0;
            }

            var total = (int)(EndTime!.Value - StartTime!.Value).TotalMinutes;

            return HasLunch
                ? total - (int)(LunchEndTime!.Value - LunchStartTime!.Value).TotalMinutes
                : total;
        }
    }

    /// <summary>Minutes of break, or zero when the day has none.</summary>
    public int LunchMinutes => HasLunch
        ? (int)(LunchEndTime!.Value - LunchStartTime!.Value).TotalMinutes
        : 0;

    /// <summary>A day with no expected work — a weekend, or a day off.</summary>
    public static WorkDaySchedule Off(DayOfWeek day) => new(day, null, null, null, null);

    /// <summary>
    /// Builds a working day, collecting every problem before returning.
    /// </summary>
    public static Result<WorkDaySchedule> Create(
        DayOfWeek day,
        TimeOnly? startTime,
        TimeOnly? lunchStartTime,
        TimeOnly? lunchEndTime,
        TimeOnly? endTime)
    {
        // Nothing filled in at all is a day off, not a malformed day.
        if (startTime is null && endTime is null && lunchStartTime is null && lunchEndTime is null)
        {
            return Result.Success(Off(day));
        }

        var errors = new List<Error>();

        if (startTime is null || endTime is null)
        {
            errors.Add(ScheduleErrors.DayIncomplete(day));
        }

        // One half of a break is not a break. Accepting it would leave the
        // calculation guessing how long it lasted.
        if (lunchStartTime is null != lunchEndTime is null)
        {
            errors.Add(ScheduleErrors.LunchIncomplete(day));
        }

        if (startTime is { } start && endTime is { } end && start >= end)
        {
            // A shift crossing midnight is a real thing this build does not
            // model. Silently accepting it would produce a negative day.
            errors.Add(ScheduleErrors.OvernightNotSupported(day));
        }

        if (lunchStartTime is { } lunchStart && lunchEndTime is { } lunchEnd)
        {
            if (lunchStart >= lunchEnd)
            {
                errors.Add(ScheduleErrors.LunchOutOfOrder(day));
            }
            else if (startTime is { } dayStart && endTime is { } dayEnd
                     && (lunchStart <= dayStart || lunchEnd >= dayEnd))
            {
                errors.Add(ScheduleErrors.LunchOutsideWorkday(day));
            }
        }

        if (errors.Count > 0)
        {
            return Result.Failure<WorkDaySchedule>(errors);
        }

        return Result.Success(
            new WorkDaySchedule(day, startTime, lunchStartTime, lunchEndTime, endTime));
    }

    /// <summary>"08:00 às 17:00, com 1h de intervalo"</summary>
    public string Describe()
    {
        if (!IsWorkday)
        {
            return "Sem expediente";
        }

        var hours = $"{StartTime:HH\\:mm} às {EndTime:HH\\:mm}";

        return HasLunch ? $"{hours}, com {DescribeLunch()} de intervalo" : hours;
    }

    private string DescribeLunch()
    {
        var minutes = LunchMinutes;

        if (minutes % 60 == 0)
        {
            return $"{minutes / 60}h";
        }

        return minutes < 60 ? $"{minutes}min" : $"{minutes / 60}h{minutes % 60:00}";
    }
}
