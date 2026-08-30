using NaHora.Domain.Abstractions;
using NaHora.Shared.Results;

namespace NaHora.Domain.Scheduling;

/// <summary>
/// The hours a company works by default.
/// </summary>
/// <remarks>
/// <para>
/// One per company. It is what the employee's punch screen shows — "das 08:00
/// às 17:00, com 1h de intervalo" — and the starting point every new hire
/// inherits until somebody gives them their own.
/// </para>
/// <para>
/// A root of its own rather than part of <c>Company</c>: it is edited on its own
/// screen, read on every punch, and loading a company's registration details to
/// answer "what time do they open" would be waste on the hottest path in the
/// product.
/// </para>
/// <para>
/// Changing it does <b>not</b> reinterpret the past. Punches already recorded
/// keep the offsets and the day they were taken under; what a manager sees for
/// last month is calculated against the schedule that was in force then, which
/// is why the per-employee override carries its own effective period.
/// </para>
/// </remarks>
[Audit]
public sealed class CompanyWorkSchedule : AggregateRoot, ITenantOwned
{
    private readonly List<WorkDaySchedule> _days = [];

    private CompanyWorkSchedule()
    {
    }

    private CompanyWorkSchedule(Guid companyId, IReadOnlyList<WorkDaySchedule> days, DateTimeOffset createdAt)
    {
        CompanyId = companyId;
        _days.AddRange(days);
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid CompanyId { get; private set; }

    /// <summary>All seven days, ordered Sunday to Saturday.</summary>
    public IReadOnlyList<WorkDaySchedule> Days => _days.AsReadOnly();

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static Result<CompanyWorkSchedule> Define(
        Guid companyId,
        IEnumerable<WorkDaySchedule>? days,
        DateTimeOffset now)
    {
        var week = WorkWeek.Complete(days);

        return week.IsFailure
            ? Result.Failure<CompanyWorkSchedule>(week.Errors)
            : Result.Success(new CompanyWorkSchedule(companyId, week.Value, now));
    }

    /// <summary>
    /// The schedule a company starts with: 08:00 to 17:00, an hour for lunch,
    /// Monday to Friday.
    /// </summary>
    public static CompanyWorkSchedule Default(Guid companyId, DateTimeOffset now) =>
        new(
            companyId,
            WorkWeek.MondayToFriday(
                new TimeOnly(8, 0),
                new TimeOnly(12, 0),
                new TimeOnly(13, 0),
                new TimeOnly(17, 0)),
            now);

    public Result Redefine(IEnumerable<WorkDaySchedule>? days, DateTimeOffset now)
    {
        var week = WorkWeek.Complete(days);
        if (week.IsFailure)
        {
            return Result.Failure(week.Errors);
        }

        _days.Clear();
        _days.AddRange(week.Value);
        UpdatedAt = now;

        return Result.Success();
    }

    public WorkDaySchedule For(DayOfWeek day) =>
        _days.FirstOrDefault(scheduled => scheduled.Day == day) ?? WorkDaySchedule.Off(day);
}
