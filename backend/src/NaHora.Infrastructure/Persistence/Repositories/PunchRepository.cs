using Microsoft.EntityFrameworkCore;
using NaHora.Domain.Attendance;
using NaHora.Shared.DependencyInjection;

namespace NaHora.Infrastructure.Persistence.Repositories;

/// <remarks>
/// Public so Wolverine's generated handler code can construct it directly; an
/// internal adapter would force service location, which the runtime rejects.
/// </remarks>
public sealed class PunchRepository(NaHoraDbContext dbContext) : IPunchRepository, IScopedService
{
    public void Add(Punch punch) => dbContext.Punches.Add(punch);

    public Task<Punch?> GetByClientIdAsync(
        Guid companyId,
        string clientId,
        CancellationToken cancellationToken = default) =>
        dbContext.Punches
            .FirstOrDefaultAsync(
                punch => punch.CompanyId == companyId && punch.ClientId == clientId,
                cancellationToken);

    public Task<Punch?> GetByIdAsync(Guid companyId, Guid punchId, CancellationToken cancellationToken = default) =>
        dbContext.Punches
            .FirstOrDefaultAsync(
                punch => punch.CompanyId == companyId && punch.Id == punchId,
                cancellationToken);

    public async Task<IReadOnlyList<Punch>> GetByLocalDateAsync(
        Guid companyId,
        Guid employeeId,
        DateOnly localDate,
        CancellationToken cancellationToken = default) =>
        await dbContext.Punches
            .Where(punch => punch.CompanyId == companyId
                            && punch.EmployeeId == employeeId
                            && punch.LocalDate == localDate)
            .OrderBy(punch => punch.OccurredAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Punch>> GetHistoryAsync(
        Guid companyId,
        Guid employeeId,
        int skip,
        int take,
        CancellationToken cancellationToken = default) =>
        await dbContext.Punches
            .Where(punch => punch.CompanyId == companyId && punch.EmployeeId == employeeId)
            .OrderByDescending(punch => punch.OccurredAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

    public Task<int> CountForEmployeeAsync(
        Guid companyId,
        Guid employeeId,
        CancellationToken cancellationToken = default) =>
        dbContext.Punches
            .CountAsync(punch => punch.CompanyId == companyId && punch.EmployeeId == employeeId, cancellationToken);

    /// <summary>
    /// Takes the next NSR, locking the company's counter row for the rest of
    /// the transaction.
    /// </summary>
    /// <remarks>
    /// <c>FOR UPDATE</c> is the whole point: two punches arriving at the same
    /// instant must not read the same last value and both write the next one.
    /// The second waits for the first to commit.
    ///
    /// The counter is created on demand, so a company's first punch does not
    /// depend on a seed having run. The insert races with another first punch,
    /// and the unique index on company is what resolves it — the loser retries
    /// the read and finds the row the winner created.
    /// </remarks>
    public async Task<long> TakeNextNsrAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var counter = await dbContext.NsrCounters
            .FromSqlInterpolated(
                $"SELECT * FROM nsr_counters WHERE company_id = {companyId} FOR UPDATE")
            .FirstOrDefaultAsync(cancellationToken);

        if (counter is null)
        {
            counter = CompanyNsrCounter.StartFor(companyId);
            dbContext.NsrCounters.Add(counter);

            // Flushed immediately so the row exists and is locked by this
            // transaction before the punch is written against its number.
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return counter.TakeNext();
    }
}
