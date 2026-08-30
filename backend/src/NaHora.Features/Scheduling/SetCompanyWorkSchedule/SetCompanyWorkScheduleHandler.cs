using NaHora.Domain.Abstractions;
using NaHora.Domain.Companies;
using NaHora.Domain.Scheduling;
using NaHora.Features.Common;
using NaHora.Shared.Results;

namespace NaHora.Features.Scheduling.SetCompanyWorkSchedule;

/// <summary>
/// Sets the company's default hours.
/// </summary>
/// <remarks>
/// Creates the schedule on the first call and replaces the week on every one
/// after. There is only ever one per company, so "define" and "redefine" are the
/// same request from the caller's side.
///
/// Changing it does not reinterpret the past: every punch already carries the
/// offset and the working day it was recorded under, and an employee with their
/// own versioned hours is unaffected.
/// </remarks>
public static class SetCompanyWorkScheduleHandler
{
    public static async Task<Result<CompanyWorkScheduleResponse>> Handle(
        SetCompanyWorkScheduleCommand command,
        ICurrentUser currentUser,
        ICompanyRepository companies,
        ICompanyWorkScheduleRepository schedules,
        IUnitOfWork unitOfWork,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(currentUser);
        ArgumentNullException.ThrowIfNull(companies);
        ArgumentNullException.ThrowIfNull(schedules);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(clock);

        var week = WorkWeekInputParser.Parse(command.Days);
        if (week.IsFailure)
        {
            return Result.Failure<CompanyWorkScheduleResponse>(week.Errors);
        }

        var companyId = currentUser.CompanyId;
        var now = clock.GetUtcNow();

        var existing = await schedules.GetByCompanyAsync(companyId, cancellationToken);

        if (existing is null)
        {
            var created = CompanyWorkSchedule.Define(companyId, week.Value, now);
            if (created.IsFailure)
            {
                return Result.Failure<CompanyWorkScheduleResponse>(created.Errors);
            }

            schedules.Add(created.Value);
            existing = created.Value;
        }
        else
        {
            var redefined = existing.Redefine(week.Value, now);
            if (redefined.IsFailure)
            {
                return Result.Failure<CompanyWorkScheduleResponse>(redefined.Errors);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var company = await companies.GetByIdAsync(companyId, cancellationToken);

        return Result.Success(new CompanyWorkScheduleResponse(
            true,
            company?.TimeZoneId ?? Company.DefaultTimeZoneId,
            [.. existing.Days.Select(WorkDayResponse.From)]));
    }
}
