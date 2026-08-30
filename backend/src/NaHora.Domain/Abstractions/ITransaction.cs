namespace NaHora.Domain.Abstractions;

/// <summary>
/// An open database transaction.
/// </summary>
/// <remarks>
/// Disposing without committing rolls back, so a handler that returns early on
/// a failed <c>Result</c> cannot accidentally leave a partial write behind.
/// </remarks>
public interface ITransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);

    Task RollbackAsync(CancellationToken cancellationToken = default);
}
