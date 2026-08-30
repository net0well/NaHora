using NaHora.Features.Common;

namespace NaHora.Features.Scheduling.GetCompanyWorkSchedule;

/// <param name="IsDefined">
/// False when nobody has set the hours yet, in which case the week returned is
/// the suggestion a form can start from rather than something already saved.
/// </param>
public sealed record GetCompanyWorkScheduleResponse(
    bool IsDefined,
    string TimeZoneId,
    IReadOnlyList<WorkDayResponse> Week);
