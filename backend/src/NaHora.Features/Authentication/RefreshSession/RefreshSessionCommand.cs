namespace NaHora.Features.Authentication.RefreshSession;

/// <summary>Exchanges a refresh token for a new pair.</summary>
public sealed record RefreshSessionCommand(string RefreshToken);
