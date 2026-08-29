namespace FaceId.Features.Authentication.RefreshSession;

/// <summary>The rotated session.</summary>
public sealed record RefreshSessionResponse(string AccessToken, string RefreshToken, int ExpiresIn);
