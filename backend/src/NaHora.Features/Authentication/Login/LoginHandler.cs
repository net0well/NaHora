using NaHora.Domain.Abstractions;
using NaHora.Domain.Authorization;
using NaHora.Domain.Companies;
using NaHora.Domain.Employees;
using NaHora.Domain.Errors;
using NaHora.Domain.Users;
using NaHora.Domain.Users.Events;
using NaHora.Domain.ValueObjects;
using NaHora.Shared.Results;
using Wolverine;

namespace NaHora.Features.Authentication.Login;

/// <summary>
/// Authenticates a CPF and password, and establishes a session.
/// </summary>
/// <remarks>
/// The entry point reads as the four questions sign-in actually asks: is the
/// company usable, are the credentials good, what session does that produce,
/// and what is still outstanding. Each is answered by its own method, so a
/// change to one does not force re-reading the others.
/// </remarks>
public static class LoginHandler
{
    /// <summary>
    /// A well-formed Argon2id hash of a value nobody knows.
    /// </summary>
    /// <remarks>
    /// Verified against when the CPF matches no account, so a missing account
    /// costs the same time as a wrong password. Without it, response time alone
    /// would reveal which CPFs work at the company — the enumeration the
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

        var companyCheck = await EnsureCompanyIsOperationalAsync(companies, command.CompanyId, cancellationToken);
        if (companyCheck.IsFailure)
        {
            return Result.Failure<LoginResponse>(companyCheck.Errors);
        }

        var authentication = await AuthenticateAsync(
            command, users, passwordHasher, bus, unitOfWork, now, cancellationToken);

        if (authentication.IsFailure)
        {
            return Result.Failure<LoginResponse>(authentication.Errors);
        }

        var user = authentication.Value;
        var employee = await employees.GetByUserIdAsync(user.CompanyId, user.Id, cancellationToken);

        var session = IssueSession(user, employee?.Id, tokenService, refreshTokens, now);

        await bus.PublishAsync(new UserSignedIn(user.CompanyId, user.Id, user.Role.ToString(), now));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(BuildResponse(user, employee, session, now));
    }

    private static async Task<Result> EnsureCompanyIsOperationalAsync(
        ICompanyRepository companies,
        Guid companyId,
        CancellationToken cancellationToken)
    {
        var company = await companies.GetByIdAsync(companyId, cancellationToken);

        // A company that does not exist is reported as bad credentials rather
        // than "no such company": the latter would confirm which identifiers
        // are real.
        return company switch
        {
            null => Result.Failure(UserErrors.InvalidCredentials()),
            { IsOperational: false } => Result.Failure(CompanyErrors.Suspended()),
            _ => Result.Success()
        };
    }

    /// <summary>
    /// Resolves the credential and returns the user, recording the attempt
    /// either way.
    /// </summary>
    private static async Task<Result<User>> AuthenticateAsync(
        LoginCommand command,
        IUserRepository users,
        IPasswordHasher passwordHasher,
        IMessageBus bus,
        IUnitOfWork unitOfWork,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (!Cpf.TryParse(command.Cpf, out var cpf))
        {
            // A malformed CPF is bad credentials rather than a validation
            // error; separating them would confirm which shapes exist.
            passwordHasher.Verify(command.Password, DummyHash);
            await bus.PublishAsync(new SignInFailed(command.CompanyId, null, command.IpAddress, now));

            return Result.Failure<User>(UserErrors.InvalidCredentials());
        }

        var user = await users.GetByCpfAsync(command.CompanyId, cpf, cancellationToken);
        if (user is null)
        {
            passwordHasher.Verify(command.Password, DummyHash);
            await bus.PublishAsync(new SignInFailed(command.CompanyId, null, command.IpAddress, now));

            return Result.Failure<User>(UserErrors.InvalidCredentials());
        }

        if (user.IsLockedAt(now))
        {
            // The one case told apart: somebody legitimate needs to know how
            // long to wait.
            return Result.Failure<User>(UserErrors.AccountLocked(user.LockedUntil!.Value - now));
        }

        if (user.Status == UserStatus.Inactive || !passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            await RecordFailedAttemptAsync(user, command.IpAddress, bus, unitOfWork, now, cancellationToken);

            return Result.Failure<User>(UserErrors.InvalidCredentials());
        }

        if (passwordHasher.NeedsRehash(user.PasswordHash))
        {
            // Upgraded silently: the owner just proved the password, so the
            // stronger hash is written without asking them for anything.
            user.UpgradePasswordHash(passwordHasher.Hash(command.Password));
        }

        user.RegisterSuccessfulLogin(now);

        return Result.Success(user);
    }

    private static async Task RecordFailedAttemptAsync(
        User user,
        string? ipAddress,
        IMessageBus bus,
        IUnitOfWork unitOfWork,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var lockDuration = user.RegisterFailedLogin(now);

        await bus.PublishAsync(new SignInFailed(user.CompanyId, user.Id, ipAddress, now));

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
    }

    private static IssuedSession IssueSession(
        User user,
        Guid? employeeId,
        IAccessTokenService tokenService,
        IRefreshTokenRepository refreshTokens,
        DateTimeOffset now)
    {
        var accessToken = tokenService.IssueAccessToken(user, employeeId);
        var refreshToken = tokenService.IssueRefreshToken();

        refreshTokens.Add(RefreshToken.Issue(
            user.CompanyId,
            user.Id,
            refreshToken.TokenHash,
            now,
            refreshToken.ExpiresAt));

        return new IssuedSession(accessToken, refreshToken);
    }

    private static LoginResponse BuildResponse(
        User user,
        Employee? employee,
        IssuedSession session,
        DateTimeOffset now) =>
        new(
            session.AccessToken.Token,
            session.RefreshToken.Token,
            (int)(session.AccessToken.ExpiresAt - now).TotalSeconds,
            new AuthenticatedUserResponse(
                user.Id,
                user.FullName,
                user.Role.ToString(),
                employee?.Id,
                RolePermissions.For(user.Role)),
            ResolvePendingActions(user, employee));

    private static List<string> ResolvePendingActions(User user, Employee? employee)
    {
        var actions = new List<string>(capacity: 2);

        if (user.MustChangePassword)
        {
            actions.Add(PendingAction.ChangePassword);
        }

        // An employee has to enrol a face before punching. Somebody without an
        // employment record never punches, so it does not apply.
        if (employee is not null)
        {
            actions.Add(PendingAction.RegisterFaceProfile);
        }

        return actions;
    }

    private readonly record struct IssuedSession(
        IssuedAccessToken AccessToken,
        IssuedRefreshToken RefreshToken);
}
