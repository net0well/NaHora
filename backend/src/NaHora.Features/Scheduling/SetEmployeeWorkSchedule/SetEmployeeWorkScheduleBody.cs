using NaHora.Features.Common;

namespace NaHora.Features.Scheduling.SetEmployeeWorkSchedule;

/// <summary>
/// The body of the request, without the employee.
/// </summary>
/// <remarks>
/// The employee comes from the route, so repeating it in the payload would
/// create two sources for one value and a question about which wins.
/// </remarks>
public sealed record SetEmployeeWorkScheduleBody(
    DateOnly? EffectiveFrom,
    IReadOnlyList<WorkDayInput>? Days);
