using NaHora.Domain.Abstractions;
using NaHora.Domain.Errors;
using NaHora.Domain.ValueObjects;
using NaHora.Shared.Results;

namespace NaHora.Domain.Users;

/// <summary>
/// An authenticable identity, scoped to one company.
/// </summary>
/// <remarks>
/// Identity is separate from the employment record: the founder who signs the
/// company up authenticates but may never punch a clock, and only an
/// <c>Employee</c> belongs in the regulatory export.
///
/// The credential is the CPF. There is no username — a second identifier for
/// the same person is a second thing to keep in sync and a second way to get it
/// wrong.
/// </remarks>
[Audit(IgnoredProperties = [nameof(LastLoginAt), nameof(FailedLoginAttempts), nameof(LockedUntil)])]
public sealed class User : AggregateRoot, ITenantOwned
{
    private User()
    {
        FullName = string.Empty;
        PasswordHash = string.Empty;
    }

    private User(
        Guid companyId,
        Cpf cpf,
        string fullName,
        EmailAddress email,
        string passwordHash,
        UserRole role,
        bool mustChangePassword,
        DateTimeOffset createdAt)
    {
        CompanyId = companyId;
        Cpf = cpf;
        FullName = fullName;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
        MustChangePassword = mustChangePassword;
        Status = UserStatus.PendingFirstAccess;
        CreatedAt = createdAt;
    }

    public Guid CompanyId { get; private set; }

    /// <summary>
    /// Immutable once set. It is the login credential and a mandatory AFD
    /// field, so an already exported record must keep matching the person.
    /// </summary>
    public Cpf Cpf { get; private set; }

    public string FullName { get; private set; }

    public EmailAddress Email { get; private set; }

    /// <summary>
    /// Argon2id hash with its parameters embedded. Never projected into a DTO,
    /// a log or the audit trail.
    /// </summary>
    public string PasswordHash { get; private set; }

    public UserRole Role { get; private set; }

    public UserStatus Status { get; private set; }

    /// <summary>True while the password was set by someone other than the owner.</summary>
    public bool MustChangePassword { get; private set; }

    public int FailedLoginAttempts { get; private set; }

    public DateTimeOffset? LockedUntil { get; private set; }

    public DateTimeOffset? LastLoginAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Result<User> Create(
        Guid companyId,
        Cpf cpf,
        string fullName,
        EmailAddress email,
        string passwordHash,
        UserRole role,
        bool mustChangePassword,
        DateTimeOffset createdAt)
    {
        var errors = new List<Error>();

        if (companyId == Guid.Empty)
        {
            errors.Add(UserErrors.CompanyRequired());
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            errors.Add(UserErrors.NameRequired());
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            errors.Add(UserErrors.PasswordRequired());
        }

        if (errors.Count > 0)
        {
            return Result.Failure<User>(errors);
        }

        return new User(companyId, cpf, fullName.Trim(), email, passwordHash, role, mustChangePassword, createdAt);
    }

    public bool IsLockedAt(DateTimeOffset now) => LockedUntil is { } until && until > now;

    /// <summary>
    /// Records a failed attempt and applies the progressive lockout.
    /// </summary>
    /// <returns>The lock duration applied, or null when none was.</returns>
    public TimeSpan? RegisterFailedLogin(DateTimeOffset now)
    {
        FailedLoginAttempts++;

        var duration = FailedLoginAttempts switch
        {
            < 5 => (TimeSpan?)null,
            5 => TimeSpan.FromMinutes(1),
            6 => TimeSpan.FromMinutes(5),
            7 => TimeSpan.FromMinutes(15),
            _ => TimeSpan.FromMinutes(60)
        };

        if (duration is { } window)
        {
            LockedUntil = now.Add(window);
        }

        return duration;
    }

    public void RegisterSuccessfulLogin(DateTimeOffset now)
    {
        FailedLoginAttempts = 0;
        LockedUntil = null;
        LastLoginAt = now;
    }

    public Result SetPassword(string passwordHash, bool mustChange)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return Result.Failure(UserErrors.PasswordRequired());
        }

        PasswordHash = passwordHash;
        MustChangePassword = mustChange;

        if (!mustChange && Status == UserStatus.PendingFirstAccess)
        {
            Status = UserStatus.Active;
        }

        return Result.Success();
    }

    /// <summary>
    /// Rehashes with current parameters after a successful sign-in, so a hash
    /// produced under weaker settings is upgraded without asking the person to
    /// do anything.
    /// </summary>
    public Result UpgradePasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return Result.Failure(UserErrors.PasswordRequired());
        }

        PasswordHash = passwordHash;

        return Result.Success();
    }

    public Result Activate()
    {
        if (Status == UserStatus.Inactive)
        {
            return Result.Failure(UserErrors.CannotActivateInactive());
        }

        Status = UserStatus.Active;

        return Result.Success();
    }

    public void Deactivate() => Status = UserStatus.Inactive;

    public void ChangeRole(UserRole role) => Role = role;

    public Result UpdateProfile(string fullName, EmailAddress email)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return Result.Failure(UserErrors.NameRequired());
        }

        FullName = fullName.Trim();
        Email = email;

        return Result.Success();
    }
}
