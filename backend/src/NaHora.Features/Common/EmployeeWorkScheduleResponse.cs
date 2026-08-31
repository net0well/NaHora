namespace NaHora.Features.Common;

/// <summary>
/// One version of an employee's own hours.
/// </summary>
/// <remarks>
/// In Common because more than one slice answers with it, and slices do not
/// reference each other.
/// </remarks>
/// <param name="EffectiveTo">Null while this version is the one in force.</param>
public sealed record EmployeeWorkScheduleResponse(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string TimeZoneId,
    IReadOnlyList<WorkDayResponse> Week);
