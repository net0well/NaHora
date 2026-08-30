using NaHora.Domain.Abstractions;
using NaHora.Domain.Adjustments;
using NaHora.Domain.Companies;
using NaHora.Domain.Employees;
using NaHora.Domain.Errors;
using NaHora.Domain.Users;
using NaHora.Features.Common;
using NaHora.Shared.Results;

namespace NaHora.Features.Adjustments.DecideAdjustment;

/// <summary>
/// A manager agrees with a correction, or declines it.
/// </summary>
/// <remarks>
/// Only from here does an adjusted time start to count. Until then the request
/// is visible to everybody involved and governs nothing, which is what lets an
/// employee ask without being able to rewrite their own record.
/// </remarks>
public static class DecideAdjustmentHandler
{
    public static async Task<Result<AdjustmentResponse>> Handle(
        DecideAdjustmentCommand command,
        ICurrentUser currentUser,
        IPunchAdjustmentRepository adjustments,
        IEmployeeRepository employees,
        IUserRepository users,
        ICompanyRepository companies,
        IUnitOfWork unitOfWork,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(currentUser);
        ArgumentNullException.ThrowIfNull(adjustments);
        ArgumentNullException.ThrowIfNull(employees);
        ArgumentNullException.ThrowIfNull(users);
        ArgumentNullException.ThrowIfNull(companies);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(clock);

        var companyId = currentUser.CompanyId;

        var adjustment = await adjustments.GetByIdAsync(companyId, command.AdjustmentId, cancellationToken);
        if (adjustment is null)
        {
            return Result.Failure<AdjustmentResponse>(AdjustmentErrors.NotFound());
        }

        var decider = await employees.GetByUserIdAsync(companyId, currentUser.UserId, cancellationToken);
        var now = clock.GetUtcNow();

        var decision = command.Approve
            ? adjustment.Approve(currentUser.UserId, decider?.Id, command.Notes, now)
            : adjustment.Reject(currentUser.UserId, decider?.Id, command.Notes, now);

        if (decision.IsFailure)
        {
            return Result.Failure<AdjustmentResponse>(decision.Errors);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var subject = await employees.GetByIdAsync(companyId, adjustment.EmployeeId, cancellationToken);
        var requester = await users.GetByIdAsync(companyId, adjustment.RequestedByUserId, cancellationToken);
        var company = await companies.GetByIdAsync(companyId, cancellationToken);
        var timeZoneId = company?.TimeZoneId ?? Company.DefaultTimeZoneId;

        return Result.Success(AdjustmentResponse.From(
            adjustment,
            subject?.FullName ?? "—",
            requester?.FullName ?? "—",
            (int)Company.OffsetAt(timeZoneId, adjustment.AdjustedOccurredAt).TotalSeconds));
    }
}
