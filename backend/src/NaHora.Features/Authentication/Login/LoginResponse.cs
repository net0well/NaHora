namespace NaHora.Features.Authentication.Login;

/// <summary>A newly established session.</summary>
public sealed record LoginResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    AuthenticatedUserResponse User,
    IReadOnlyList<string> PendingActions);
