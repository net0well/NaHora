namespace NaHora.Domain.Abstractions;

/// <summary>
/// Commits a set of changes as one atomic operation.
/// </summary>
/// <remarks>
/// The port exists for the cases where atomicity is a business rule rather than
/// an implementation detail. Allocating an NSR is the clearest one: the counter
/// increment and the attendance record must commit together, because a
/// consumed-then-rolled-back number would leave a gap, and Ordinance 671/2021
/// requires the sequence to have none.
///
/// Explicit transactions are for operations that span more than one aggregate.
/// A single-aggregate change only needs <see cref="SaveChangesAsync"/>, which
/// is already atomic.
/// </remarks>
public interface IUnitOfWork
{
    /// <summary>Persists every pending change.</summary>
    /// <returns>Number of affected rows.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens an explicit transaction. Only needed when the work spans multiple
    /// aggregates or requires row locking.
    /// </summary>
    Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
