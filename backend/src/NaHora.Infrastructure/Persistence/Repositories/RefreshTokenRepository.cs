using Microsoft.EntityFrameworkCore;
using NaHora.Domain.Users;
using NaHora.Shared.DependencyInjection;

namespace NaHora.Infrastructure.Persistence.Repositories;

/// <remarks>
/// Public so Wolverine's generated handler code can construct it directly; an
/// internal adapter would force service location, which the runtime rejects.
/// </remarks>
public sealed class RefreshTokenRepository(NaHoraDbContext dbContext)
    : IRefreshTokenRepository, IScopedService
{
    /// <summary>
    /// Bypasses the tenant filter deliberately: the caller presenting the token
    /// has no session yet, so there is no ambient company to filter by. The
    /// company travels on the token row itself, and that is the value the rest
    /// of the flow trusts.
    /// </summary>
    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        dbContext.RefreshTokens
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

    public Task<int> RevokeFamilyAsync(
        Guid familyId,
        DateTimeOffset revokedAt,
        CancellationToken cancellationToken = default) =>
        dbContext.RefreshTokens
            .IgnoreQueryFilters()
            .Where(token => token.FamilyId == familyId && token.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.RevokedAt, revokedAt), cancellationToken);

    public Task<int> RevokeAllForUserAsync(
        Guid userId,
        DateTimeOffset revokedAt,
        CancellationToken cancellationToken = default) =>
        dbContext.RefreshTokens
            .IgnoreQueryFilters()
            .Where(token => token.UserId == userId && token.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.RevokedAt, revokedAt), cancellationToken);

    public void Add(RefreshToken refreshToken) => dbContext.RefreshTokens.Add(refreshToken);
}
