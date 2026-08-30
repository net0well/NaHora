using NaHora.Shared.Results;

namespace NaHora.Domain.Errors;

/// <summary>
/// Failures produced while defining a working schedule.
/// </summary>
/// <remarks>
/// Every one of these refuses a <b>definition</b>. None of them ever refuses a
/// punch: a schedule says what was expected, and Portaria 671, Art. 78 §1º
/// forbids turning an expectation into a gate.
/// </remarks>
public static class ScheduleErrors
{
    public static Error DayIncomplete(DayOfWeek day) => Error.Validation(
        "SCHEDULE_DAY_INCOMPLETE",
        "A working day needs both a start and an end time.",
        Field(day));

    public static Error LunchIncomplete(DayOfWeek day) => Error.Validation(
        "SCHEDULE_LUNCH_INCOMPLETE",
        "A break needs both a start and an end time.",
        Field(day));

    public static Error LunchOutOfOrder(DayOfWeek day) => Error.Validation(
        "SCHEDULE_LUNCH_OUT_OF_ORDER",
        "The break must end after it starts.",
        Field(day));

    public static Error LunchOutsideWorkday(DayOfWeek day) => Error.Validation(
        "SCHEDULE_LUNCH_OUTSIDE_WORKDAY",
        "The break must fall inside the working day.",
        Field(day));

    public static Error OvernightNotSupported(DayOfWeek day) => Error.Validation(
        "SCHEDULE_OVERNIGHT_NOT_SUPPORTED",
        "A shift crossing midnight is not supported yet.",
        Field(day));

    public static Error WeekIncomplete() => Error.Validation(
        "SCHEDULE_WEEK_INCOMPLETE",
        "A schedule must define all seven days of the week.",
        "days");

    public static Error NotFound() => Error.NotFound(
        "SCHEDULE_NOT_FOUND",
        "No schedule was found.");

    public static Error EffectiveFromInThePast() => Error.Validation(
        "SCHEDULE_EFFECTIVE_FROM_IN_THE_PAST",
        "A schedule cannot start before the one it replaces.",
        "effectiveFrom");

    /// <summary>
    /// The field a validation error points at, so a form can highlight the day
    /// that is wrong rather than the whole week.
    /// </summary>
    private static string Field(DayOfWeek day) => $"days[{(int)day}]";
}
