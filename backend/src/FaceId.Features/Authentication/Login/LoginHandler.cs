using FaceId.Domain.Abstractions;
using FaceId.Domain.Authorization;
using FaceId.Domain.Companies;
using FaceId.Domain.Employees;
using FaceId.Domain.Errors;
using FaceId.Domain.Users;
using FaceId.Domain.Users.Events;
using FaceId.Domain.ValueObjects;
using FaceId.Shared.Results;
using Wolverine;

namespace FaceId.Features.Authentication.Login;

/// <summary>
/// Authenticates a CPF and password, and establishes a session.
/// </summary>
public static class LoginHandler
{
    /// <summary>
    /// A well-formed Argon2id hash of a value nobody knows.
    /// </summary>
    /// <remarks>
    /// Verified against when the CPF does not exist, so a missing account costs
    /// the same time as a wrong password. Without it, response time alone would
    /// tell an attacker which CPFs work at the company — the enumeration the
    /// identical error message exists to prevent.
    /// </remarks>
    private const string DummyHash =
        "$argon2id$v=19$m=65536,t=3,p=2$c29tZVNhbHRWYWx1ZTEyMw==$dGhpc0lzTm90QVJlYWxIYXNoVmFsdWVBdEFsbA==";

    public static async Task<Result<LoginResponse>> Handle(
        LoginCommand command,
        IUserRepository users,
        ICompanyRepository companies,
        IEmployeeRepository employees,
        IRefreshTokenRepository refreshTokens,
        IPasswordHasher passwordHasher,
        IAccessTokenService tokenService,
        IMessageBus bus,
        IUnitOfWork unitOfWork,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var now = clock.GetUtcNow();

        var company = await companies.GetByIdAsync(command.CompanyId, cancellationToken);
        if (company is null || !company.IsOperational)
        {
            return Result.Failure<LoginResponse>(
                company is null ? UserErrors.InvalidCredentials() : CompanyErrors.Suspended());
        }

        if (!Cpf.TryParse(command.Cpf, out var cpf))
        {
            // Malformed CPF is reported as bad credentials, not as a validation
            // error: distinguishing them would confirm which shapes exist.
            passwordHasher.Verify(command.Password, DummyHash);
            return Result.Failure<LoginResponse>(UserErrors.InvalidCredentials());
        }

        var user = await users.GetByCpfAsync(command.CompanyId, cpf, cancellationToken);
        if (user is null)
        {
            passwordHasher.Verify(command.Password, DummyHash);

            // Recorded with a null user: the audit trail may know the CPF
            // matched nothing, the HTTP response may not.
            await bus.PublishAsync(new SignInFailed(command.CompanyId, null, command.IpAddress, now));

            return Result.Failure<LoginResponse>(UserErrors.InvalidCredentials());
        }

        if (user.IsLockedAt(now))
        {
            // The one case that is told apart: a legitimate user needs to know
            // how long to wait.
            return Result.Failure<LoginResponse>(
                UserErrors.AccountLocked(user.LockedUntil!.Value - now));
        }

        if (user.Status == UserStatus.Inactive || !passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            var lockDuration = user.RegisterFailedLogin(now);

            await bus.PublishAsync(new SignInFailed(user.CompanyId, user.Id, command.IpAddress, now));

            if (lockDuration is not null)
            {
                await bus.PublishAsync(new AccountLocked(
                    user.CompanyId,
                    user.Id,
                    user.FailedLoginAttempts,
                    user.LockedUntil!.Value,
                    now));
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Failure<LoginResponse>(UserErrors.InvalidCredentials());
        }

        if (passwordHasher.NeedsRehash(user.PasswordHash))
        {
            // Upgraded silently: the owner proved the password just now, so the
            // stronger hash can be written without asking them for anything.
            user.UpgradePasswordHash(passwordHasher.Hash(command.Password));
        }

        user.RegisterSuccessfulLogin(now);

        var employee = await employees.GetByUserIdAsync(user.CompanyId, user.Id, cancellationToken);

        var accessToken = tokenService.IssueAccessToken(user, employee?.Id);
        var refreshToken = tokenService.IssueRefreshToken();

        refreshTokens.Add(RefreshToken.Issue(
            user.CompanyId,
            user.Id,
            refreshToken.TokenHash,
            now,
            refreshToken.ExpiresAt));

        await bus.PublishAsync(new UserSignedIn(user.CompanyId, user.Id, user.Role.ToString(), now));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new LoginResponse(
            accessToken.Token,
            refreshToken.Token,
            (int)(accessToken.ExpiresAt - now).TotalSeconds,
            new AuthenticatedUserResponse(
                user.Id,
                user.FullName,
                user.Role.ToString(),
                employee?.Id,
                RolePermissions.For(user.Role)),
            ResolvePendingActions(user, employee)));
    }

    private static List<string> ResolvePendingActions(User user, Employee? employee)
    {
        var actions = new List<string>(capacity: 2);

        if (user.MustChangePassword)
        {
            actions.Add(PendingAction.ChangePassword);
        }

        // An employee has to enrol a face before they can punch. Someone
        // without an employment record never punches, so it does not apply.
        if (employee is not null)
        {
            actions.Add(PendingAction.RegisterFaceProfile);
        }

        return actions;
    }
}
