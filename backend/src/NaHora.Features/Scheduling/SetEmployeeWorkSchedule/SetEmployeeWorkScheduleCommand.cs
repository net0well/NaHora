using NaHora.Features.Common;

namespace NaHora.Features.Scheduling.SetEmployeeWorkSchedule;

/// <summary>
/// Gives one employee hours of their own, from a date onwards.
/// </summary>
/// <param name="EffectiveFrom">
/// When the new hours start. Null means today. The version in force is closed
/// the day before, so a day already worked keeps the schedule it was worked
/// under.
/// </param>
public sealed record SetEmployeeWorkScheduleCommand(
    Guid EmployeeId,
    DateOnly? EffectiveFrom,
    IReadOnlyList<WorkDayInput>? Days);
