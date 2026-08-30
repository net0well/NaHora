using NaHora.Features.Common;

namespace NaHora.Features.Scheduling.GetMyWorkSchedule;

/// <summary>
/// The effective week, and today singled out.
/// </summary>
/// <param name="Source">
/// <c>Employee</c> when the person has their own hours, <c>Company</c> when
/// they work the default, <c>None</c> when nobody has set any. Shown so an
/// employee can tell "my hours" from "the company's hours" without asking.
/// </param>
/// <param name="Today">
/// The day the punch screen prints. Resolved on the server in company time,
/// because the device's idea of the date is what this product does not trust.
/// </param>
public sealed record GetMyWorkScheduleResponse(
    string Source,
    string TimeZoneId,
    DateOnly LocalDate,
    WorkDayResponse Today,
    IReadOnlyList<WorkDayResponse> Week);
