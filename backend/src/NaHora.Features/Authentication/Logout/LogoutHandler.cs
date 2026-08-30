using NaHora.Domain.Abstractions;
using NaHora.Domain.Authorization;
using NaHora.Domain.Users;
using NaHora.Shared.Results;

namespace NaHora.Features.Authentication.Logout;

/// <summary>
/// Revokes a session.
/// </summary>
/// <remarks>
/// Revoking the refresh token is what makes logout real. The access token
/// stays valid until it expires, which is why its lifetime is minutes rather
/// than hours.
/// </remarks>
public static class LogoutHandler
{
    public static async Task<Result> Handle(
        LogoutCommand command,
        IRefreshTokenRepository refreshTokens,
        IAccessTokenService tokenService,
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var now = clock.GetUtcNow();

        if (command.AllSessions)
        {
            await refreshTokens.RevokeAllForUserAsync(currentUser.UserId, now, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }

        if (string.IsNullOrWhiteSpace(command.RefreshToken))
        {
            // Nothing to revoke is not an error: logging out twice, or from a
            // client that already dropped its token, should still succeed.
            return Result.Success();
        }

        var stored = await refreshTokens.GetByHashAsync(
            tokenService.HashRefreshToken(command.RefreshToken),
            cancellationToken);

        // Only the owner may revoke it. Without this check a leaked token could
        // be used to log somebody else out.
        if (stored is not null && stored.UserId == currentUser.UserId)
        {
            stored.Revoke(now);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
