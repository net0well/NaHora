using NaHora.Domain.Abstractions;
using NaHora.Domain.Companies;
using NaHora.Domain.Scheduling;
using NaHora.Features.Common;
using NaHora.Shared.Results;

namespace NaHora.Features.Scheduling.GetCompanyWorkSchedule;

/// <summary>
/// The company's default hours, for the screen that edits them.
/// </summary>
/// <remarks>
/// Answers with the suggested week when nothing has been defined, flagged as
/// not yet saved. A blank form is a worse starting point than the shape almost
/// every Brazilian office actually uses.
/// </remarks>
public static class GetCompanyWorkScheduleHandler
{
    public static async Task<Result<CompanyWorkScheduleResponse>> Handle(
        GetCompanyWorkScheduleQuery query,
        ICurrentUser currentUser,
        ICompanyRepository companies,
        ICompanyWorkScheduleRepository schedules,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(currentUser);
        ArgumentNullException.ThrowIfNull(companies);
        ArgumentNullException.ThrowIfNull(schedules);
        ArgumentNullException.ThrowIfNull(clock);

        var companyId = currentUser.CompanyId;

        var company = await companies.GetByIdAsync(companyId, cancellationToken);
        var schedule = await schedules.GetByCompanyAsync(companyId, cancellationToken);

        var week = (schedule ?? CompanyWorkSchedule.Default(companyId, clock.GetUtcNow())).Days;

        return Result.Success(new CompanyWorkScheduleResponse(
            schedule is not null,
            company?.TimeZoneId ?? Company.DefaultTimeZoneId,
            [.. week.Select(WorkDayResponse.From)]));
    }
}
