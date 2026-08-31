using NaHora.Domain.Abstractions;
using NaHora.Domain.Adjustments;
using NaHora.Domain.Attendance;
using NaHora.Domain.Companies;
using NaHora.Domain.Employees;
using NaHora.Domain.Errors;
using NaHora.Domain.Scheduling;
using NaHora.Shared.Results;

namespace NaHora.Features.Attendance.GetMyPunches;

/// <summary>
/// The signed-in employee's marks for one working day, as they count.
/// </summary>
/// <remarks>
/// What the app shows on the home screen: the marks already made today and
/// which boundary the next one would be. The day is resolved in company time on
/// the server — a phone whose date is a day out would otherwise ask for a day
/// that never happened and be told, correctly, that it is empty.
///
/// Approved treatments are applied on the way out. The punches table is not
/// touched: the row goes signed into the AFD, and a correction is a treatment
/// beside it rather than an edit to it.
/// </remarks>
public static class GetMyPunchesHandler
{
    public static async Task<Result<GetMyPunchesResponse>> Handle(
        GetMyPunchesQuery query,
        ICurrentUser currentUser,
        IPunchRepository punches,
        IPunchAdjustmentRepository adjustments,
        IEmployeeRepository employees,
        ICompanyRepository companies,
        ICompanyWorkScheduleRepository companySchedules,
        IEmployeeWorkScheduleRepository employeeSchedules,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(currentUser);
        ArgumentNullException.ThrowIfNull(punches);
        ArgumentNullException.ThrowIfNull(adjustments);
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
        var (dayStartUtc, dayEndUtc) = Company.LocalDayWindow(timeZoneId, localDate);

        var day = await punches.GetByLocalDateAsync(companyId, employee.Id, localDate, cancellationToken);

        var treatments = await Treatments(
            adjustments, companyId, employee.Id, day, dayStartUtc, dayEndUtc, cancellationToken);

        var record = EffectivePunchRecord.Merge(
            day,
            treatments,
            instant => Company.OffsetAt(timeZoneId, instant));

        // What the day was supposed to look like. It never gates the next
        // punch — it is what lets the app say "you have already recorded the
        // day" before somebody taps again by accident.
        var scheduled = EffectiveWorkDay.On(
            localDate,
            await employeeSchedules.GetForEmployeeAsync(companyId, employee.Id, cancellationToken),
            await companySchedules.GetByCompanyAsync(companyId, cancellationToken));

        return Result.Success(new GetMyPunchesResponse(
            localDate,
            timeZoneId,

            // Counted over the record as it stands, inclusions included. An
            // approved inclusion is a mark of the day, so it has to move what
            // comes next — otherwise the app offers "Entrada" to somebody whose
            // entry was just granted.
            PunchDirectionSequence.NextAfter(record.Count).ToString(),
            scheduled.ExpectedPunches,
            scheduled.ExpectedPunches > 0 && record.Count >= scheduled.ExpectedPunches,
            [.. record.Select(ToResponse)]));
    }

    /// <summary>
    /// The treatments that can touch this day: corrections aimed at its punches,
    /// and inclusions that land inside it.
    /// </summary>
    /// <remarks>
    /// Two queries because they are two different questions. A correction is
    /// found through the punch it corrects; an inclusion has no punch, so the
    /// only thing to look it up by is when it says it happened.
    /// </remarks>
    private static async Task<IReadOnlyList<PunchAdjustment>> Treatments(
        IPunchAdjustmentRepository adjustments,
        Guid companyId,
        Guid employeeId,
        IReadOnlyList<Punch> day,
        DateTimeOffset dayStartUtc,
        DateTimeOffset dayEndUtc,
        CancellationToken cancellationToken)
    {
        var corrections = await adjustments.GetForPunchesAsync(
            companyId,
            [.. day.Select(punch => punch.Id)],
            cancellationToken);

        var inclusions = await adjustments.GetInclusionsBetweenAsync(
            companyId,
            employeeId,
            dayStartUtc,
            dayEndUtc,
            cancellationToken);

        return [.. corrections, .. inclusions];
    }

    private static DayPunchResponse ToResponse(EffectivePunch entry) =>
        new(
            entry.Id,
            entry.Nsr,
            entry.Direction.ToString(),
            entry.Origin?.ToString(),
            entry.Trust?.ToString(),
            entry.Source.ToString(),
            entry.OccurredAt,
            entry.OriginalOccurredAt,
            (int)entry.CompanyOffset.TotalSeconds,
            entry.AccuracyMeters);
}
