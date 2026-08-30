using NaHora.Domain.Abstractions;
using NaHora.Domain.Companies;
using NaHora.Domain.Employees;
using NaHora.Domain.Errors;
using NaHora.Domain.Scheduling;
using NaHora.Features.Common;
using NaHora.Shared.Results;

namespace NaHora.Features.Scheduling.SetEmployeeWorkSchedule;

/// <summary>
/// Gives one employee hours of their own.
/// </summary>
/// <remarks>
/// Opens a new version and closes the one in force the day before it starts.
/// Editing the row in place instead would judge every day already worked
/// against hours that were not in force at the time — months of punctuality
/// turning into retroactive lateness the moment somebody moves a start time.
/// </remarks>
public static class SetEmployeeWorkScheduleHandler
{
    public static async Task<Result<EmployeeWorkScheduleResponse>> Handle(
        SetEmployeeWorkScheduleCommand command,
        ICurrentUser currentUser,
        IEmployeeRepository employees,
        ICompanyRepository companies,
        IEmployeeWorkScheduleRepository schedules,
        IUnitOfWork unitOfWork,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(currentUser);
        ArgumentNullException.ThrowIfNull(employees);
        ArgumentNullException.ThrowIfNull(companies);
        ArgumentNullException.ThrowIfNull(schedules);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(clock);

        var companyId = currentUser.CompanyId;

        // 404, not 403: confirming that an employee exists in another company
        // is itself a leak.
        var employee = await employees.GetByIdAsync(companyId, command.EmployeeId, cancellationToken);
        if (employee is null)
        {
            return Result.Failure<EmployeeWorkScheduleResponse>(EmployeeErrors.NotFound());
        }

        var week = WorkWeekInputParser.Parse(command.Days);
        if (week.IsFailure)
        {
            return Result.Failure<EmployeeWorkScheduleResponse>(week.Errors);
        }

        var now = clock.GetUtcNow();
        var company = await companies.GetByIdAsync(companyId, cancellationToken);
        var timeZoneId = company?.TimeZoneId ?? Company.DefaultTimeZoneId;

        var effectiveFrom = command.EffectiveFrom ?? Company.LocalDateAt(timeZoneId, now);

        var current = await schedules.GetCurrentAsync(companyId, employee.Id, cancellationToken);
        if (current is not null)
        {
            var closed = current.CloseBefore(effectiveFrom);
            if (closed.IsFailure)
            {
                return Result.Failure<EmployeeWorkScheduleResponse>(closed.Errors);
            }
        }

        var defined = EmployeeWorkSchedule.Define(companyId, employee.Id, week.Value, effectiveFrom, now);
        if (defined.IsFailure)
        {
            return Result.Failure<EmployeeWorkScheduleResponse>(defined.Errors);
        }

        schedules.Add(defined.Value);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new EmployeeWorkScheduleResponse(
            defined.Value.Id,
            employee.Id,
            employee.FullName,
            defined.Value.EffectiveFrom,
            defined.Value.EffectiveTo,
            timeZoneId,
            [.. defined.Value.Days.Select(WorkDayResponse.From)]));
    }
}
