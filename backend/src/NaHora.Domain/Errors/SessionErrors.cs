using NaHora.Shared.Results;

namespace NaHora.Domain.Errors;

/// <summary>
/// Failures of the session lifecycle.
/// </summary>
public static class SessionErrors
{
    public static Error RefreshTokenInvalid() => Error.Unauthorized(
        "REFRESH_TOKEN_INVALID",
        "Session is no longer valid. Sign in again.");

    /// <summary>
    /// A token that was already rotated has been presented again. The classic
    /// signature of a stolen credential: the legitimate holder and the thief
    /// cannot both continue, so the whole family is revoked.
    /// </summary>
    public static Error RefreshTokenReused() => Error.Unauthorized(
        "REFRESH_TOKEN_REUSED",
        "Session is no longer valid. Sign in again.");
}
