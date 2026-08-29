using FaceId.Domain.Abstractions;
using Microsoft.EntityFrameworkCore.Storage;

namespace FaceId.Infrastructure.Persistence;

/// <summary>
/// Wraps an EF Core transaction behind the domain port.
/// </summary>
/// <remarks>
/// Disposing an uncommitted transaction rolls it back, which is what makes an
/// early return on a failed <c>Result</c> safe: the handler does not have to
/// remember to undo anything.
/// </remarks>
internal sealed class EfCoreTransaction(IDbContextTransaction transaction) : ITransaction
{
    public Task CommitAsync(CancellationToken cancellationToken = default) =>
        transaction.CommitAsync(cancellationToken);

    public Task RollbackAsync(CancellationToken cancellationToken = default) =>
        transaction.RollbackAsync(cancellationToken);

    public ValueTask DisposeAsync() => transaction.DisposeAsync();
}
