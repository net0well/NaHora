using NaHora.Domain.Abstractions;
using NaHora.Domain.Companies;
using NaHora.Domain.Employees;
using NaHora.Domain.Errors;
using NaHora.Domain.Scheduling;
using NaHora.Features.Common;
using NaHora.Shared.Results;

namespace NaHora.Features.Scheduling.GetMyWorkSchedule;

/// <summary>
/// The hours the signed-in person is expected to work.
/// </summary>
/// <remarks>
/// What the punch screen prints above the button — "das 08:00 às 17:00, com 1h
/// de intervalo" — so somebody about to record a punch can see what the day was
/// supposed to be. It is information, never a gate.
/// </remarks>
public static class GetMyWorkScheduleHandler
{
    public static async Task<Result<GetMyWorkScheduleResponse>> Handle(
        GetMyWorkScheduleQuery query,
        ICurrentUser currentUser,
        IEmployeeRepository employees,
        ICompanyRepository companies,
        ICompanyWorkScheduleRepository companySchedules,
        IEmployeeWorkScheduleRepository employeeSchedules,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(currentUser);
        ArgumentNullException.ThrowIfNull(employees);
        ArgumentNullException.ThrowIfNull(companies);
        ArgumentNullException.ThrowIfNull(companySchedules);
        ArgumentNullException.ThrowIfNull(employeeSchedules);
        ArgumentNullException.ThrowIfNull(clock);

        var companyId = currentUser.CompanyId;

        var employee = await employees.GetByUserIdAsync(companyId, currentUser.UserId, cancellationToken);
        if (employee is null)
        {
            return Result.Failure<GetMyWorkScheduleResponse>(PunchErrors.EmployeeRequired());
        }

        var company = await companies.GetByIdAsync(companyId, cancellationToken);
        var timeZoneId = company?.TimeZoneId ?? Company.DefaultTimeZoneId;
        var localDate = Company.LocalDateAt(timeZoneId, clock.GetUtcNow());

        var companySchedule = await companySchedules.GetByCompanyAsync(companyId, cancellationToken);
        var own = await employeeSchedules.GetForEmployeeAsync(companyId, employee.Id, cancellationToken);

        var governing = own.FirstOrDefault(schedule => schedule.AppliesOn(localDate));

        // The whole week comes from whichever schedule governs today, so the
        // employee reads one consistent week rather than a mix of their own
        // days and the company's.
        var week = Enum.GetValues<DayOfWeek>()
            .OrderBy(day => day)
            .Select(day => governing?.For(day) ?? companySchedule?.For(day) ?? WorkDaySchedule.Off(day))
            .Select(WorkDayResponse.From)
            .ToList();

        var source = governing is not null
            ? "Employee"
            : companySchedule is not null ? "Company" : "None";

        return Result.Success(new GetMyWorkScheduleResponse(
            source,
            timeZoneId,
            localDate,
            week[(int)localDate.DayOfWeek],
            week));
    }
}
