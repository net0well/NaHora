using NaHora.Features.Common;

namespace NaHora.Features.Scheduling.SetCompanyWorkSchedule;

/// <summary>
/// Defines, or redefines, the hours a company works by default.
/// </summary>
/// <remarks>
/// Carries no company: it comes from the token, like everywhere else. All seven
/// days must be present — a partial edit that silently left Saturday out would
/// turn it into an unpaid working day.
/// </remarks>
public sealed record SetCompanyWorkScheduleCommand(IReadOnlyList<WorkDayInput>? Days);
