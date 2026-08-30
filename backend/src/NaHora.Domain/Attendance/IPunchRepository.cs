namespace NaHora.Domain.Attendance;

/// <summary>
/// Persistence for the punch aggregate.
/// </summary>
public interface IPunchRepository
{
    void Add(Punch punch);

    /// <summary>
    /// Finds a punch by the identifier the device generated.
    /// </summary>
    /// <remarks>
    /// The idempotency lookup. A client that retries after a timeout asks for
    /// the same identifier, and getting the original back is what stops the
    /// retry from becoming a second punch.
    /// </remarks>
    Task<Punch?> GetByClientIdAsync(Guid companyId, string clientId, CancellationToken cancellationToken = default);

    Task<Punch?> GetByIdAsync(Guid companyId, Guid punchId, CancellationToken cancellationToken = default);

    /// <summary>The employee's punches for one local day, in the order they happened.</summary>
    Task<IReadOnlyList<Punch>> GetByLocalDateAsync(
        Guid companyId,
        Guid employeeId,
        DateOnly localDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The employee's punches, newest first, one page at a time.
    /// </summary>
    Task<IReadOnlyList<Punch>> GetHistoryAsync(
        Guid companyId,
        Guid employeeId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<int> CountForEmployeeAsync(Guid companyId, Guid employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Takes the next NSR for the company, locking the counter row.
    /// </summary>
    /// <remarks>
    /// Serialises punches within one company for the duration of the
    /// transaction. Acceptable: a company punching two thousand times a day
    /// concentrated into a few minutes still peaks at tens per second, far
    /// below what one contended row handles.
    /// </remarks>
    Task<long> TakeNextNsrAsync(Guid companyId, CancellationToken cancellationToken = default);
}
