using FaceId.Shared.Results;

namespace FaceId.Domain.Errors;

/// <summary>
/// Failures of the user aggregate.
/// </summary>
public static class UserErrors
{
    public static Error CompanyRequired() => Error.Validation(
        "USER_COMPANY_REQUIRED",
        "A user must belong to a company.",
        "companyId");

    public static Error NameRequired() => Error.Validation(
        "USER_NAME_REQUIRED",
        "Full name is required.",
        "fullName");

    public static Error PasswordRequired() => Error.Validation(
        "USER_PASSWORD_REQUIRED",
        "A password hash is required.",
        "password");

    public static Error NotFound() => Error.NotFound(
        "USER_NOT_FOUND",
        "User was not found.");

    public static Error CpfAlreadyRegistered() => Error.Conflict(
        "CPF_ALREADY_REGISTERED",
        "This CPF is already registered in the company.",
        "cpf");

    public static Error EmailAlreadyRegistered() => Error.Conflict(
        "EMAIL_ALREADY_REGISTERED",
        "This email address is already registered.",
        "email");

    /// <summary>
    /// Deliberately identical for an unknown CPF, a wrong password and an
    /// inactive account. Telling them apart would let an attacker enumerate who
    /// works at the company.
    /// </summary>
    public static Error InvalidCredentials() => Error.Unauthorized(
        "INVALID_CREDENTIALS",
        "CPF or password is incorrect.");

    public static Error AccountLocked(TimeSpan retryAfter) => Error.Forbidden(
        "ACCOUNT_LOCKED",
        $"Too many attempts. Try again in {retryAfter.TotalMinutes:0} minute(s).");

    public static Error CannotActivateInactive() => Error.Conflict(
        "USER_CANNOT_ACTIVATE",
        "An inactive user cannot be reactivated.");

    public static Error OnboardingIncomplete() => Error.Forbidden(
        "ONBOARDING_INCOMPLETE",
        "Complete your first access before continuing.");
}
