using FaceId.Domain.Abstractions;
using FaceId.Shared.DependencyInjection;

namespace FaceId.Infrastructure.Persistence;

/// <summary>
/// Unit of work backed by the EF Core change tracker.
/// </summary>
/// <remarks>
/// A thin adapter on purpose: <see cref="FaceIdDbContext"/> already implements
/// the pattern. The port exists so handlers can express "commit these changes
/// together" without taking a dependency on EF Core, and so the NSR allocation
/// can hold a real transaction across two aggregates.
///
/// Public so Wolverine's generated handler code can construct it directly; an
/// internal adapter would force service location, which the runtime rejects.
/// </remarks>
public sealed class UnitOfWork(FaceIdDbContext dbContext) : IUnitOfWork, IScopedService
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);

    public async Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        return new EfCoreTransaction(transaction);
    }
}
