using NaHora.Domain.Abstractions;
using NaHora.Domain.Adjustments;
using NaHora.Domain.Authorization;
using NaHora.Domain.Companies;
using NaHora.Domain.Employees;
using NaHora.Domain.Errors;
using NaHora.Domain.Users;
using NaHora.Features.Common;
using NaHora.Shared.Results;

namespace NaHora.Features.Adjustments.GetAdjustments;

/// <summary>
/// Corrections the caller may see.
/// </summary>
/// <remarks>
/// A manager sees the company's; everybody else sees their own and nobody
/// else's. The difference is decided here rather than by a parameter, because a
/// parameter is something a caller can change.
/// </remarks>
public static class GetAdjustmentsHandler
{
    public static async Task<Result<IReadOnlyList<AdjustmentResponse>>> Handle(
        GetAdjustmentsQuery query,
        ICurrentUser currentUser,
        IPunchAdjustmentRepository adjustments,
        IEmployeeRepository employees,
        IUserRepository users,
        ICompanyRepository companies,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(currentUser);
        ArgumentNullException.ThrowIfNull(adjustments);
        ArgumentNullException.ThrowIfNull(employees);
        ArgumentNullException.ThrowIfNull(users);
        ArgumentNullException.ThrowIfNull(companies);

        var companyId = currentUser.CompanyId;
        var canDecide = currentUser.HasPermission(Permissions.AdjustAttendance);

        IReadOnlyList<PunchAdjustment> found;

        if (canDecide)
        {
            found = query.PendingOnly
                ? await adjustments.GetPendingAsync(companyId, cancellationToken)
                : await adjustments.GetForCompanyAsync(companyId, cancellationToken);
        }
        else
        {
            var caller = await employees.GetByUserIdAsync(companyId, currentUser.UserId, cancellationToken);
            if (caller is null)
            {
                return Result.Failure<IReadOnlyList<AdjustmentResponse>>(PunchErrors.EmployeeRequired());
            }

            found = await adjustments.GetForEmployeeAsync(companyId, caller.Id, cancellationToken);
        }

        var company = await companies.GetByIdAsync(companyId, cancellationToken);
        var timeZoneId = company?.TimeZoneId ?? Company.DefaultTimeZoneId;

        var responses = new List<AdjustmentResponse>(found.Count);

        // Names are looked up once each and remembered. A queue is usually a
        // handful of people asking several times, and querying per row would
        // turn twenty requests into forty round trips.
        var employeeNames = new Dictionary<Guid, string>();
        var userNames = new Dictionary<Guid, string>();

        foreach (var adjustment in found)
        {
            if (!employeeNames.TryGetValue(adjustment.EmployeeId, out var subjectName))
            {
                var subject = await employees.GetByIdAsync(companyId, adjustment.EmployeeId, cancellationToken);
                subjectName = subject?.FullName ?? "—";
                employeeNames[adjustment.EmployeeId] = subjectName;
            }

            if (!userNames.TryGetValue(adjustment.RequestedByUserId, out var requesterName))
            {
                var requester = await users.GetByIdAsync(companyId, adjustment.RequestedByUserId, cancellationToken);
                requesterName = requester?.FullName ?? "—";
                userNames[adjustment.RequestedByUserId] = requesterName;
            }

            responses.Add(AdjustmentResponse.From(
                adjustment,
                subjectName,
                requesterName,
                (int)Company.OffsetAt(timeZoneId, adjustment.AdjustedOccurredAt).TotalSeconds));
        }

        return Result.Success<IReadOnlyList<AdjustmentResponse>>(responses);
    }
}
