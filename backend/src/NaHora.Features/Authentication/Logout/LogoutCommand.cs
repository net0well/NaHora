namespace NaHora.Features.Authentication.Logout;

/// <summary>Ends a session.</summary>
/// <param name="RefreshToken">The token to revoke. Ignored when <paramref name="AllSessions"/> is set.</param>
/// <param name="AllSessions">Revokes every session of the user instead of just this one.</param>
public sealed record LogoutCommand(string? RefreshToken, bool AllSessions = false);
