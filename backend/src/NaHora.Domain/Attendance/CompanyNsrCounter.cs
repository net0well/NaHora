using NaHora.Domain.Abstractions;

namespace NaHora.Domain.Attendance;

/// <summary>
/// The last NSR handed out for a company.
/// </summary>
/// <remarks>
/// A table row rather than a PostgreSQL sequence, and the reason is the one
/// thing sequences do differently: they do not roll back. A transaction that
/// takes a number and then aborts leaves that number spent, and the AFD
/// requires a sequence with no gaps — an auditor reading 1487 followed by 1489
/// asks what happened to 1488, and "the transaction failed" is not an answer
/// the file can carry.
///
/// Incremented under a row lock in the same transaction as the punch, so the
/// number and the record commit or fail together.
/// </remarks>
public sealed class CompanyNsrCounter : Entity
{
    private CompanyNsrCounter()
    {
    }

    public Guid CompanyId { get; private set; }

    /// <summary>The highest NSR already assigned. Starts at zero.</summary>
    public long LastNsr { get; private set; }

    public static CompanyNsrCounter StartFor(Guid companyId) =>
        new() { CompanyId = companyId, LastNsr = 0 };

    /// <summary>
    /// Takes the next number.
    /// </summary>
    /// <remarks>
    /// Safe only while the row is locked. The lock is the repository's job,
    /// because it is a persistence concern and this type has no way to know
    /// whether it holds one.
    /// </remarks>
    public long TakeNext() => ++LastNsr;
}
