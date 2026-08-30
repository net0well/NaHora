using System.Net.Mail;

using NaHora.Domain.Errors;
using NaHora.Shared.Results;

namespace NaHora.Domain.ValueObjects;

/// <summary>
/// An email address, normalised to lower case.
/// </summary>
/// <remarks>
/// Named <c>EmailAddress</c> rather than <c>Email</c> so it never collides with
/// the message type this project will need for notifications.
///
/// Validation delegates to <see cref="MailAddress"/> instead of a regular
/// expression. Every hand-written email regex is wrong in one direction or the
/// other, and here the cost of a false rejection is a person locked out of
/// their own attendance record.
/// </remarks>
public readonly record struct EmailAddress
{
    private const int MaxLength = 254;

    private EmailAddress(string value) => Value = value;

    public string Value { get; }

    /// <summary>
    /// Parses the input, reporting an invalid value as a failure rather than an
    /// exception: a mistyped document is an expected outcome of a form, not an
    /// exceptional condition.
    /// </summary>
    public static Result<EmailAddress> Create(string? input, string? field = null) =>
        TryParse(input, out var email)
            ? Result.Success(email)
            : Result.Failure<EmailAddress>(ValueObjectErrors.EmailInvalid(field));

    public static bool TryParse(string? input, out EmailAddress email)
    {
        email = default;

        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var trimmed = input.Trim();
        if (trimmed.Length > MaxLength)
        {
            return false;
        }

        if (!MailAddress.TryCreate(trimmed, out var parsed))
        {
            return false;
        }

        // MailAddress accepts a display name ("Maria <maria@acme.com>"), which
        // is not an address. Requiring an exact round trip rejects that.
        if (!string.Equals(parsed.Address, trimmed, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // A domain without a dot is syntactically legal but never routable on
        // the public internet.
        if (!parsed.Host.Contains('.', StringComparison.Ordinal))
        {
            return false;
        }

        email = new EmailAddress(trimmed.ToLowerInvariant());
        return true;
    }

    public override string ToString() => Value;

    public static implicit operator string(EmailAddress email) => email.Value;
}
