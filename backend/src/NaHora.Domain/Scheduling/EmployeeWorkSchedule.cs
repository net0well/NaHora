using NaHora.Domain.Abstractions;
using NaHora.Domain.Errors;
using NaHora.Shared.Results;

namespace NaHora.Domain.Scheduling;

/// <summary>
/// One employee's own hours, for a period.
/// </summary>
/// <remarks>
/// <para>
/// Overrides the company default for whoever it belongs to. An employee without
/// one simply works the company's hours.
/// </para>
/// <para>
/// <b>Versioned, never edited in place.</b> Changing somebody's hours closes the
/// current period and opens a new one, so a day worked in March is always judged
/// against the schedule that was in force in March. Rewriting the row instead
/// would turn months of punctuality into retroactive lateness the moment
/// somebody moves the start time.
/// </para>
/// <para>
/// The period covers the whole week rather than a single weekday. Spec 003
/// models it per day; a weekly version is what an operator actually does
/// ("from June this person works these hours"), it makes the no-overlap rule a
/// single comparison instead of seven, and nothing in the product needs to move
/// only Wednesdays.
/// </para>
/// </remarks>
[Audit]
public sealed class EmployeeWorkSchedule : AggregateRoot, ITenantOwned
{
    private readonly List<WorkDaySchedule> _days = [];

    private EmployeeWorkSchedule()
    {
    }

    private EmployeeWorkSchedule(
        Guid companyId,
        Guid employeeId,
        IReadOnlyList<WorkDaySchedule> days,
        DateOnly effectiveFrom,
        DateTimeOffset createdAt)
    {
        CompanyId = companyId;
        EmployeeId = employeeId;
        _days.AddRange(days);
        EffectiveFrom = effectiveFrom;
        CreatedAt = createdAt;
    }

    public Guid CompanyId { get; private set; }

    public Guid EmployeeId { get; private set; }

    public IReadOnlyList<WorkDaySchedule> Days => _days.AsReadOnly();

    public DateOnly EffectiveFrom { get; private set; }

    /// <summary>The last day this version applies to. Null while it is current.</summary>
    public DateOnly? EffectiveTo { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public bool IsCurrent => EffectiveTo is null;

    public static Result<EmployeeWorkSchedule> Define(
        Guid companyId,
        Guid employeeId,
        IEnumerable<WorkDaySchedule>? days,
        DateOnly effectiveFrom,
        DateTimeOffset now)
    {
        var week = WorkWeek.Complete(days);

        return week.IsFailure
            ? Result.Failure<EmployeeWorkSchedule>(week.Errors)
            : Result.Success(new EmployeeWorkSchedule(companyId, employeeId, week.Value, effectiveFrom, now));
    }

    /// <summary>Whether this version governs a given day.</summary>
    public bool AppliesOn(DateOnly date) =>
        date >= EffectiveFrom && (EffectiveTo is null || date <= EffectiveTo);

    /// <summary>
    /// Closes this version the day before its replacement starts.
    /// </summary>
    /// <remarks>
    /// Refuses to close before it opened, because a period that ends before it
    /// begins governs nothing and would leave the day it was meant to cover
    /// with no schedule at all.
    /// </remarks>
    public Result CloseBefore(DateOnly replacementStartsOn)
    {
        var lastDay = replacementStartsOn.AddDays(-1);

        if (lastDay < EffectiveFrom)
        {
            return Result.Failure(ScheduleErrors.EffectiveFromInThePast());
        }

        EffectiveTo = lastDay;

        return Result.Success();
    }

    public WorkDaySchedule For(DayOfWeek day) =>
        _days.FirstOrDefault(scheduled => scheduled.Day == day) ?? WorkDaySchedule.Off(day);
}
