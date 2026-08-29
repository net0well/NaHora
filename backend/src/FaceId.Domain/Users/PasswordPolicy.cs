using System.Globalization;
using FaceId.Domain.Errors;
using FaceId.Domain.ValueObjects;
using FaceId.Shared.Results;

namespace FaceId.Domain.Users;

/// <summary>
/// The rules a password must satisfy.
/// </summary>
/// <remarks>
/// There is no forced expiry. Rotating passwords on a calendar pushes people
/// towards predictable variations of one password, which is worse than keeping
/// a strong one.
/// </remarks>
public static class PasswordPolicy
{
    public const int MinimumLength = 10;

    private const int RequiredCharacterClasses = 3;

    /// <summary>
    /// Validates a candidate password, reporting every unmet rule at once.
    /// </summary>
    /// <param name="password">The candidate.</param>
    /// <param name="cpf">Owner's CPF, rejected as a substring.</param>
    /// <param name="fullName">Owner's name; its parts are rejected as substrings.</param>
    /// <param name="email">Owner's email; the local part is rejected as a substring.</param>
    public static Result Validate(string? password, Cpf cpf, string fullName, EmailAddress email)
    {
        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(password) || password.Length < MinimumLength)
        {
            errors.Add(UserErrors.PasswordTooShort(MinimumLength));
            return Result.Failure(errors);
        }

        if (CountCharacterClasses(password) < RequiredCharacterClasses)
        {
            errors.Add(UserErrors.PasswordTooSimple(RequiredCharacterClasses));
        }

        if (ContainsPersonalData(password, cpf, fullName, email))
        {
            errors.Add(UserErrors.PasswordContainsPersonalData());
        }

        return errors.Count > 0 ? Result.Failure(errors) : Result.Success();
    }

    private static int CountCharacterClasses(string password)
    {
        var hasLower = false;
        var hasUpper = false;
        var hasDigit = false;
        var hasSymbol = false;

        foreach (var character in password)
        {
            if (char.IsLower(character))
            {
                hasLower = true;
            }
            else if (char.IsUpper(character))
            {
                hasUpper = true;
            }
            else if (char.IsDigit(character))
            {
                hasDigit = true;
            }
            else
            {
                hasSymbol = true;
            }
        }

        return (hasLower ? 1 : 0) + (hasUpper ? 1 : 0) + (hasDigit ? 1 : 0) + (hasSymbol ? 1 : 0);
    }

    private static bool ContainsPersonalData(string password, Cpf cpf, string fullName, EmailAddress email)
    {
        var candidate = password.ToLower(CultureInfo.InvariantCulture);

        if (candidate.Contains(cpf.Value, StringComparison.Ordinal))
        {
            return true;
        }

        var localPart = email.Value.Split('@')[0];
        if (localPart.Length >= 4 && candidate.Contains(localPart, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Any name part of four characters or more. Shorter fragments would
        // reject reasonable passwords for anyone with a short name.
        foreach (var part in fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (part.Length >= 4 && candidate.Contains(part, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
