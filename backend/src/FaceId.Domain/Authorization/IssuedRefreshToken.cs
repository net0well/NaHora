namespace FaceId.Domain.Authorization;

/// <summary>
/// A refresh token in both forms: the value returned to the client, and the
/// hash persisted in its place.
/// </summary>
/// <remarks>
/// Carries its own expiry so callers never need the token configuration, which
/// lives in Infrastructure. A handler reaching for it would invert the
/// dependency direction.
/// </remarks>
public sealed record IssuedRefreshToken(string Token, string TokenHash, DateTimeOffset ExpiresAt);
