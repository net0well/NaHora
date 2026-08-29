using FaceId.Domain.Users;

namespace FaceId.Domain.Authorization;

/// <summary>
/// Issues the short-lived access token and the opaque refresh token.
/// </summary>
public interface IAccessTokenService
{
    /// <summary>Signs an access token carrying the caller's identity and permissions.</summary>
    IssuedAccessToken IssueAccessToken(User user, Guid? employeeId);

    /// <summary>
    /// Produces a refresh token: the value handed to the client, and the hash
    /// that is stored. The plaintext is never persisted, so a database dump
    /// cannot be replayed as a live session.
    /// </summary>
    IssuedRefreshToken IssueRefreshToken();

    /// <summary>Hashes a presented refresh token so it can be looked up.</summary>
    string HashRefreshToken(string token);
}
