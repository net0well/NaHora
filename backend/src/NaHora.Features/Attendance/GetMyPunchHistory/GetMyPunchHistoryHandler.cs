using NaHora.Domain.Abstractions;
using NaHora.Domain.Attendance;
using NaHora.Domain.Employees;
using NaHora.Domain.Errors;
using NaHora.Shared.Results;

namespace NaHora.Features.Attendance.GetMyPunchHistory;

/// <summary>
/// A page of the signed-in employee's own punches.
/// </summary>
/// <remarks>
/// The employee's copy of their record. Portaria 671 gives them the right to
/// their own marks, and a list they can scroll is how that right is exercised
/// in practice rather than on paper.
/// </remarks>
public static class GetMyPunchHistoryHandler
{
    public static async Task<Result<GetMyPunchHistoryResponse>> Handle(
        GetMyPunchHistoryQuery query,
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

        var employee = await employees.GetByUserIdAsync(companyId, currentUser.UserId, cancellationToken);
        if (employee is null)
        {
            return Result.Failure<GetMyPunchHistoryResponse>(PunchErrors.EmployeeRequired());
        }

        var total = await punches.CountForEmployeeAsync(companyId, employee.Id, cancellationToken);

        var page = await punches.GetHistoryAsync(
            companyId,
            employee.Id,
            query.Skip,
            query.Take,
            cancellationToken);

        return Result.Success(new GetMyPunchHistoryResponse(
            query.NormalisedPage,
            query.NormalisedPageSize,
            total,
            query.Skip + page.Count < total,
            [.. page.Select(ToResponse)]));
    }

    private static PunchHistoryEntryResponse ToResponse(Punch punch) =>
        new(
            punch.Id,
            punch.Nsr,
            punch.Direction.ToString(),
            punch.Origin.ToString(),
            punch.Trust.ToString(),
            punch.OccurredAt,
            punch.LocalDate,
            punch.Location.AccuracyMeters);
}
