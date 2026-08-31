using NaHora.Domain.Abstractions;
using NaHora.Domain.Adjustments;
using NaHora.Domain.Attendance;
using NaHora.Domain.Companies;
using NaHora.Domain.Employees;
using NaHora.Domain.Errors;
using NaHora.Shared.Results;

namespace NaHora.Features.Attendance.GetMyPunchHistory;

/// <summary>
/// A page of the signed-in employee's own record, as it counts.
/// </summary>
/// <remarks>
/// The employee's copy of their record. Portaria 671 gives them the right to
/// their own marks, and a list they can scroll is how that right is exercised
/// in practice rather than on paper.
///
/// Approved treatments are applied on the way out, so the times here are the
/// ones that govern. The punches table is untouched — a correction is a
/// treatment beside the row, never an edit to it.
/// </remarks>
public static class GetMyPunchHistoryHandler
{
    public static async Task<Result<GetMyPunchHistoryResponse>> Handle(
        GetMyPunchHistoryQuery query,
        ICurrentUser currentUser,
        IPunchRepository punches,
        IPunchAdjustmentRepository adjustments,
        IEmployeeRepository employees,
        ICompanyRepository companies,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(currentUser);
        ArgumentNullException.ThrowIfNull(punches);
        ArgumentNullException.ThrowIfNull(adjustments);
        ArgumentNullException.ThrowIfNull(employees);
        ArgumentNullException.ThrowIfNull(companies);

        var companyId = currentUser.CompanyId;

        var employee = await employees.GetByUserIdAsync(companyId, currentUser.UserId, cancellationToken);
        if (employee is null)
        {
            return Result.Failure<GetMyPunchHistoryResponse>(PunchErrors.EmployeeRequired());
        }

        var company = await companies.GetByIdAsync(companyId, cancellationToken);
        var timeZoneId = company?.TimeZoneId ?? Company.DefaultTimeZoneId;

        var total = await punches.CountForEmployeeAsync(companyId, employee.Id, cancellationToken);

        var page = await punches.GetHistoryAsync(
            companyId,
            employee.Id,
            query.Skip,
            query.Take,
            cancellationToken);

        var hasMore = query.Skip + page.Count < total;

        var treatments = await Treatments(
            adjustments,
            companyId,
            employee.Id,
            page,
            InclusionWindow(timeZoneId, page, query.NormalisedPage, hasMore),
            cancellationToken);

        var record = EffectivePunchRecord.Merge(
            page,
            treatments,
            instant => Company.OffsetAt(timeZoneId, instant));

        return Result.Success(new GetMyPunchHistoryResponse(
            query.NormalisedPage,
            query.NormalisedPageSize,

            // Counts punches, not entries. An inclusion has no punch behind it,
            // so counting it here would make the paging arithmetic disagree
            // with the table it pages over.
            total,
            hasMore,

            // Newest first, matching the order the page was read in. The merge
            // sorts ascending because that is the order a working day reads in.
            [.. record.Reverse().Select(ToResponse)]));
    }

    /// <summary>
    /// The UTC span whose inclusions belong on this page.
    /// </summary>
    /// <remarks>
    /// Bounded by the working days the page's punches cover, because an
    /// inclusion has no punch to be paged alongside — the only thing to place
    /// it by is when it says it happened.
    ///
    /// The first page runs to the end of time, so an inclusion granted for a
    /// day with no punch at all still appears. The last page runs back to the
    /// beginning, so nothing is unreachable.
    ///
    /// A working day split across two pages shows its inclusions on both. That
    /// is a pagination artefact rather than a duplicate record, and it is
    /// visible on a screen that groups by day.
    /// </remarks>
    private static (DateTimeOffset StartUtc, DateTimeOffset EndUtc) InclusionWindow(
        string timeZoneId,
        IReadOnlyList<Punch> page,
        int pageNumber,
        bool hasMore)
    {
        var isFirstPage = pageNumber <= 1;

        if (page.Count == 0)
        {
            // Nothing to bound by. Either the employee has no punches at all,
            // or the caller paged past the end; in both cases anything granted
            // to them belongs here rather than nowhere.
            return (DateTimeOffset.MinValue, DateTimeOffset.MaxValue);
        }

        var oldest = page.Min(punch => punch.LocalDate);
        var newest = page.Max(punch => punch.LocalDate);

        var start = hasMore
            ? Company.LocalDayWindow(timeZoneId, oldest).StartUtc
            : DateTimeOffset.MinValue;

        var end = isFirstPage
            ? DateTimeOffset.MaxValue
            : Company.LocalDayWindow(timeZoneId, newest).EndUtc;

        return (start, end);
    }

    private static async Task<IReadOnlyList<PunchAdjustment>> Treatments(
        IPunchAdjustmentRepository adjustments,
        Guid companyId,
        Guid employeeId,
        IReadOnlyList<Punch> page,
        (DateTimeOffset StartUtc, DateTimeOffset EndUtc) window,
        CancellationToken cancellationToken)
    {
        var corrections = await adjustments.GetForPunchesAsync(
            companyId,
            [.. page.Select(punch => punch.Id)],
            cancellationToken);

        var inclusions = await adjustments.GetInclusionsBetweenAsync(
            companyId,
            employeeId,
            window.StartUtc,
            window.EndUtc,
            cancellationToken);

        return [.. corrections, .. inclusions];
    }

    private static PunchHistoryEntryResponse ToResponse(EffectivePunch entry) =>
        new(
            entry.Id,
            entry.Nsr,
            entry.Direction.ToString(),
            entry.Origin?.ToString(),
            entry.Trust?.ToString(),
            entry.Source.ToString(),
            entry.OccurredAt,
            entry.OriginalOccurredAt,
            entry.LocalDate,
            (int)entry.CompanyOffset.TotalSeconds,
            entry.AccuracyMeters);
}
