using NaHora.Domain.Abstractions;
using NaHora.Domain.Attendance;
using NaHora.Domain.Authorization;
using NaHora.Domain.Employees;
using NaHora.Domain.Errors;
using NaHora.Shared.Results;

namespace NaHora.Features.Attendance.GetPunchLocation;

/// <summary>
/// Where one mark was made, so a screen can show it on a map.
/// </summary>
/// <remarks>
/// The only route that answers with a coordinate. Everything that lists punches
/// deliberately does not: a listing that carried positions would put a map of
/// where somebody has been on the device most likely to be lost or taken, and
/// nobody needs that to read their own hours.
///
/// A mark belonging to another company answers 404 rather than 403. Telling an
/// outsider that the identifier exists is already telling them something.
/// </remarks>
public static class GetPunchLocationHandler
{
    public static async Task<Result<PunchLocationResponse>> Handle(
        GetPunchLocationQuery query,
        ICurrentUser currentUser,
        IPunchRepository punches,
        IEmployeeRepository employees,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(currentUser);
        ArgumentNullException.ThrowIfNull(punches);
        ArgumentNullException.ThrowIfNull(employees);

        var companyId = currentUser.CompanyId;

        var punch = await punches.GetByIdAsync(companyId, query.PunchId, cancellationToken);
        if (punch is null)
        {
            return Result.Failure<PunchLocationResponse>(PunchErrors.NotFound());
        }

        if (!await MayRead(punch, currentUser, employees, companyId, cancellationToken))
        {
            // Also 404: an employee who may not read a colleague's mark must not
            // learn from the status code that the mark is there.
            return Result.Failure<PunchLocationResponse>(PunchErrors.NotFound());
        }

        return Result.Success(new PunchLocationResponse(
            punch.Id,
            punch.Location.Latitude,
            punch.Location.Longitude,
            punch.Location.AccuracyMeters,
            punch.Location.CapturedAt,
            punch.OccurredAt,
            (int)punch.CompanyOffset.TotalSeconds));
    }

    private static async Task<bool> MayRead(
        Punch punch,
        ICurrentUser currentUser,
        IEmployeeRepository employees,
        Guid companyId,
        CancellationToken cancellationToken)
    {
        if (currentUser.HasPermission(Permissions.ViewAllAttendance))
        {
            return true;
        }

        var employee = await employees.GetByUserIdAsync(companyId, currentUser.UserId, cancellationToken);

        return employee is not null && employee.Id == punch.EmployeeId;
    }
}
