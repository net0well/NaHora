using NaHora.Domain.Abstractions;
using NaHora.Domain.Attendance;
using NaHora.Domain.Companies;
using NaHora.Domain.Employees;
using NaHora.Domain.Errors;
using NaHora.Shared.Results;

namespace NaHora.Features.Attendance.GetMyPunches;

/// <summary>
/// The signed-in employee's punches for one working day.
/// </summary>
/// <remarks>
/// What the app shows on the home screen: the marks already made today and
/// which boundary the next one would be. The day is resolved in company time on
/// the server — a phone whose date is a day out would otherwise ask for a day
/// that never happened and be told, correctly, that it is empty.
/// </remarks>
public static class GetMyPunchesHandler
{
    public static async Task<Result<GetMyPunchesResponse>> Handle(
        GetMyPunchesQuery query,
        ICurrentUser currentUser,
        IPunchRepository punches,
        IEmployeeRepository employees,
        ICompanyRepository companies,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(currentUser);
        ArgumentNullException.ThrowIfNull(punches);
        ArgumentNullException.ThrowIfNull(employees);
        ArgumentNullException.ThrowIfNull(companies);
        ArgumentNullException.ThrowIfNull(clock);

        var companyId = currentUser.CompanyId;

        var employee = await employees.GetByUserIdAsync(companyId, currentUser.UserId, cancellationToken);
        if (employee is null)
        {
            return Result.Failure<GetMyPunchesResponse>(PunchErrors.EmployeeRequired());
        }

        var company = await companies.GetByIdAsync(companyId, cancellationToken);
        var timeZoneId = company?.TimeZoneId ?? Company.DefaultTimeZoneId;

        var localDate = query.LocalDate ?? Company.LocalDateAt(timeZoneId, clock.GetUtcNow());

        var day = await punches.GetByLocalDateAsync(companyId, employee.Id, localDate, cancellationToken);

        return Result.Success(new GetMyPunchesResponse(
            localDate,
            timeZoneId,
            PunchDirectionSequence.NextAfter(day.Count).ToString(),
            [.. day.Select(ToResponse)]));
    }

    private static DayPunchResponse ToResponse(Punch punch) =>
        new(
            punch.Id,
            punch.Nsr,
            punch.Direction.ToString(),
            punch.Origin.ToString(),
            punch.Trust.ToString(),
            punch.OccurredAt,
            punch.Location.AccuracyMeters);
}
