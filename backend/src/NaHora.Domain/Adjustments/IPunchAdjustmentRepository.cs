namespace NaHora.Domain.Adjustments;

/// <summary>
/// Persistence for punch treatments.
/// </summary>
public interface IPunchAdjustmentRepository
{
    void Add(PunchAdjustment adjustment);

    Task<PunchAdjustment?> GetByIdAsync(Guid companyId, Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Everything a manager still has to decide, oldest request first.
    /// </summary>
    /// <remarks>
    /// Oldest first because a queue that shows the newest on top leaves the
    /// oldest request permanently at the bottom.
    /// </remarks>
    Task<IReadOnlyList<PunchAdjustment>> GetPendingAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);

    /// <summary>Everything in the company, newest request first.</summary>
    Task<IReadOnlyList<PunchAdjustment>> GetForCompanyAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);

    /// <summary>Everything asked for on one employee's record, newest first.</summary>
    Task<IReadOnlyList<PunchAdjustment>> GetForEmployeeAsync(
        Guid companyId,
        Guid employeeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Treatments that touch a set of punches, so a day can be rendered with
    /// the times that actually count.
    /// </summary>
    Task<IReadOnlyList<PunchAdjustment>> GetForPunchesAsync(
        Guid companyId,
        IReadOnlyCollection<Guid> punchIds,
        CancellationToken cancellationToken = default);

    /// <summary>Approved inclusions for one working day, which have no punch behind them.</summary>
    Task<IReadOnlyList<PunchAdjustment>> GetInclusionsForDayAsync(
        Guid companyId,
        Guid employeeId,
        DateTimeOffset dayStartUtc,
        DateTimeOffset dayEndUtc,
        CancellationToken cancellationToken = default);
}
