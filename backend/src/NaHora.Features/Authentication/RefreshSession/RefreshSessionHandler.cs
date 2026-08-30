using NaHora.Domain.Abstractions;
using NaHora.Domain.Authorization;
using NaHora.Domain.Companies;
using NaHora.Domain.Employees;
using NaHora.Domain.Errors;
using NaHora.Domain.Users;
using NaHora.Domain.Users.Events;
using NaHora.Shared.Results;
using Wolverine;

namespace NaHora.Features.Authentication.RefreshSession;

/// <summary>
/// Rotates a session: every refresh issues a new pair and revokes the one
/// presented.
/// </summary>
public static class RefreshSessionHandler
{
    public static async Task<Result<RefreshSessionResponse>> Handle(
        RefreshSessionCommand command,
        IRefreshTokenRepository refreshTokens,
        IUserRepository users,
        ICompanyRepository companies,
        IEmployeeRepository employees,
        IAccessTokenService tokenService,
        IMessageBus bus,
        IUnitOfWork unitOfWork,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var now = clock.GetUtcNow();

        if (string.IsNullOrWhiteSpace(command.RefreshToken))
        {
            return Result.Failure<RefreshSessionResponse>(SessionErrors.RefreshTokenInvalid());
        }

        var hash = tokenService.HashRefreshToken(command.RefreshToken);
        var stored = await refreshTokens.GetByHashAsync(hash, cancellationToken);

        if (stored is null)
        {
            return Result.Failure<RefreshSessionResponse>(SessionErrors.RefreshTokenInvalid());
        }

        // A token that was already rotated is being presented again. Either the
        // legitimate holder replayed an old value, or somebody stole one — and
        // the two cannot be told apart, so the whole chain goes.
        if (stored.IsRevoked)
        {
            var revoked = await refreshTokens.RevokeFamilyAsync(stored.FamilyId, now, cancellationToken);

            await bus.PublishAsync(new RefreshTokenReuseDetected(
                stored.CompanyId,
                stored.UserId,
                stored.FamilyId,
                revoked,
                now));

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Failure<RefreshSessionResponse>(SessionErrors.RefreshTokenReused());
        }

        if (!stored.IsActiveAt(now))
        {
            return Result.Failure<RefreshSessionResponse>(SessionErrors.RefreshTokenInvalid());
        }

        // Re-checked on every rotation, which is what makes the short access
        // lifetime the real revocation window for a suspended company.
        var company = await companies.GetByIdAsync(stored.CompanyId, cancellationToken);
        if (company is null || !company.IsOperational)
        {
            return Result.Failure<RefreshSessionResponse>(CompanyErrors.Suspended());
        }

        var user = await users.GetByIdAsync(stored.CompanyId, stored.UserId, cancellationToken);
        if (user is null || user.Status == UserStatus.Inactive)
        {
            return Result.Failure<RefreshSessionResponse>(SessionErrors.RefreshTokenInvalid());
        }

        var employee = await employees.GetByUserIdAsync(user.CompanyId, user.Id, cancellationToken);

        var accessToken = tokenService.IssueAccessToken(user, employee?.Id);
        var rotated = tokenService.IssueRefreshToken();

        refreshTokens.Add(stored.Rotate(rotated.TokenHash, now, rotated.ExpiresAt));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new RefreshSessionResponse(
            accessToken.Token,
            rotated.Token,
            (int)(accessToken.ExpiresAt - now).TotalSeconds));
    }
}
