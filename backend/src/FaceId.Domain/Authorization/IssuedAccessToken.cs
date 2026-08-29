namespace FaceId.Domain.Authorization;

/// <summary>A signed access token and when it stops being valid.</summary>
public sealed record IssuedAccessToken(string Token, DateTimeOffset ExpiresAt);
