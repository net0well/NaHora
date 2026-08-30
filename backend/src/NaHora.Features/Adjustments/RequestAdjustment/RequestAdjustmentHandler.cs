using NaHora.Domain.Abstractions;
using NaHora.Domain.Adjustments;
using NaHora.Domain.Attendance;
using NaHora.Domain.Authorization;
using NaHora.Domain.Companies;
using NaHora.Domain.Employees;
using NaHora.Domain.Errors;
using NaHora.Domain.Users;
using NaHora.Features.Common;
using NaHora.Shared.Results;

namespace NaHora.Features.Adjustments.RequestAdjustment;

/// <summary>
/// Asks for a time record to be corrected.
/// </summary>
/// <remarks>
/// <para>
/// An employee may ask about their own record and nobody else's. A manager may
/// ask about anybody's, and when the record is not their own the request is
/// approved in the same breath — they already hold the authority a second step
/// would be checking.
/// </para>
/// <para>
/// Nothing here touches the punch. The record stays exactly as it was taken,
/// and what changes is which time governs once somebody agrees.
/// </para>
/// </remarks>
public static class RequestAdjustmentHandler
{
    public static async Task<Result<AdjustmentResponse>> Handle(
        RequestAdjustmentCommand command,
        ICurrentUser currentUser,
        IEmployeeRepository employees,
        IUserRepository users,
        ICompanyRepository companies,
        IPunchRepository punches,
        IPunchAdjustmentRepository adjustments,
        IUnitOfWork unitOfWork,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(currentUser);
        ArgumentNullException.ThrowIfNull(employees);
        ArgumentNullException.ThrowIfNull(users);
        ArgumentNullException.ThrowIfNull(companies);
        ArgumentNullException.ThrowIfNull(punches);
        ArgumentNullException.ThrowIfNull(adjustments);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(clock);

        var companyId = currentUser.CompanyId;
        var canDecide = currentUser.HasPermission(Permissions.AdjustAttendance);

        var caller = await employees.GetByUserIdAsync(companyId, currentUser.UserId, cancellationToken);

        // Whose record this touches. Only somebody who may decide can name
        // another person; for everybody else it is their own or nothing.
        var subject = command.EmployeeId is { } requested && canDecide
            ? await employees.GetByIdAsync(companyId, requested, cancellationToken)
            : caller;

        if (subject is null)
        {
            return Result.Failure<AdjustmentResponse>(EmployeeErrors.NotFound());
        }

        if (!canDecide && subject.Id != caller?.Id)
        {
            // 404 rather than 403: telling somebody the record exists is
            // already more than they are entitled to know.
            return Result.Failure<AdjustmentResponse>(EmployeeErrors.NotFound());
        }

        var justification = AdjustmentJustificationCatalogue.All
            .FirstOrDefault(entry => entry.Id == command.JustificationId);

        if (justification is null)
        {
            return Result.Failure<AdjustmentResponse>(AdjustmentErrors.JustificationNotFound());
        }

        var now = clock.GetUtcNow();
        var company = await companies.GetByIdAsync(companyId, cancellationToken);
        var timeZoneId = company?.TimeZoneId ?? Company.DefaultTimeZoneId;

        var built = await BuildAsync(
            command, companyId, subject, justification, currentUser.UserId, now, punches, cancellationToken);

        if (built.IsFailure)
        {
            return Result.Failure<AdjustmentResponse>(built.Errors);
        }

        var adjustment = built.Value;

        // A manager acting on somebody else's record decides now. On their own
        // they cannot, and the request waits for another pair of eyes.
        if (canDecide)
        {
            adjustment.Approve(currentUser.UserId, caller?.Id, decisionNotes: null, now);
        }

        adjustments.Add(adjustment);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var requester = await users.GetByIdAsync(companyId, currentUser.UserId, cancellationToken);

        return Result.Success(AdjustmentResponse.From(
            adjustment,
            subject.FullName,
            requester?.FullName ?? "—",
            (int)Company.OffsetAt(timeZoneId, adjustment.AdjustedOccurredAt).TotalSeconds));
    }

    /// <summary>
    /// A correction when a punch was named, an inclusion when none was.
    /// </summary>
    private static async Task<Result<PunchAdjustment>> BuildAsync(
        RequestAdjustmentCommand command,
        Guid companyId,
        Employee subject,
        AdjustmentJustification justification,
        Guid requestedByUserId,
        DateTimeOffset now,
        IPunchRepository punches,
        CancellationToken cancellationToken)
    {
        if (command.OccurredAt is not { } occurredAt)
        {
            return Result.Failure<PunchAdjustment>(AdjustmentErrors.OccurredAtRequired());
        }

        if (command.PunchId is not { } punchId)
        {
            var direction = ParseDirection(command.Direction);

            return direction.IsFailure
                ? Result.Failure<PunchAdjustment>(direction.Errors)
                : PunchAdjustment.RequestInclusion(
                    companyId, subject.Id, occurredAt, direction.Value,
                    justification, command.Notes, requestedByUserId, now);
        }

        var punch = await punches.GetByIdAsync(companyId, punchId, cancellationToken);

        if (punch is null || punch.EmployeeId != subject.Id)
        {
            return Result.Failure<PunchAdjustment>(AdjustmentErrors.PunchNotFound());
        }

        return PunchAdjustment.RequestTimeChange(
            companyId, subject.Id, punch, occurredAt,
            justification, command.Notes, requestedByUserId, now);
    }

    private static Result<PunchDirection> ParseDirection(string? value) =>
        Enum.TryParse<PunchDirection>(value, ignoreCase: true, out var direction)
            ? Result.Success(direction)
            : Result.Failure<PunchDirection>(AdjustmentErrors.DirectionUnknown(value));
}
