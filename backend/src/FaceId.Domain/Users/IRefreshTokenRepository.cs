namespace FaceId.Domain.Users;

/// <summary>
/// Access to session credentials.
/// </summary>
public interface IRefreshTokenRepository
{
    /// <summary>
    /// Resolves a presented token by its hash.
    /// </summary>
    /// <remarks>
    /// Not scoped to a company: the caller has no session yet, and the token
    /// itself carries the company it belongs to. That value is what the rest of
    /// the flow trusts.
    /// </remarks>
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes every token in a chain. Called when a rotated token is presented
    /// again, which means one of the two holders is not the legitimate one.
    /// </summary>
    Task<int> RevokeFamilyAsync(Guid familyId, DateTimeOffset revokedAt, CancellationToken cancellationToken = default);

    /// <summary>Revokes every active session of a user. Used by logout-all and password reset.</summary>
    Task<int> RevokeAllForUserAsync(Guid userId, DateTimeOffset revokedAt, CancellationToken cancellationToken = default);

    void Add(RefreshToken refreshToken);
}
