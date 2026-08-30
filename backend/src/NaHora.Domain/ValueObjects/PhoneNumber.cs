using NaHora.Domain.Errors;
using NaHora.Shared.Results;

namespace NaHora.Domain.ValueObjects;

/// <summary>
/// Brazilian phone number, with area code.
/// </summary>
/// <remarks>
/// Ten or eleven digits: two for the area code, then eight for a landline or
/// nine for a mobile. Stored without punctuation so the same number typed two
/// ways compares equal.
///
/// Deliberately not validating the area code against the list of real ones. The
/// list changes, and rejecting a number because this build's copy is out of
/// date would keep somebody from finishing a form over a detail nobody checks.
/// </remarks>
public readonly record struct PhoneNumber
{
    private const int LandlineLength = 10;
    private const int MobileLength = 11;

    private PhoneNumber(string value) => Value = value;

    /// <summary>Ten or eleven digits, no punctuation.</summary>
    public string Value { get; }

    public bool IsMobile => Value.Length == MobileLength;

    public static Result<PhoneNumber> Create(string? input, string? field = null) =>
        TryParse(input, out var phone)
            ? Result.Success(phone)
            : Result.Failure<PhoneNumber>(ValueObjectErrors.PhoneNumberInvalid(field));

    public static bool TryParse(string? input, out PhoneNumber phone)
    {
        phone = default;

        var digits = Normalize(input);
        if (digits is null)
        {
            return false;
        }

        phone = new PhoneNumber(digits);

        return true;
    }

    /// <summary>"(11) 98765-4321"</summary>
    public string Formatted
    {
        get
        {
            var area = Value[..2];
            var rest = Value[2..];
            var split = rest.Length - 4;

            return $"({area}) {rest[..split]}-{rest[split..]}";
        }
    }

    public override string ToString() => Value;

    private static string? Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        Span<char> digits = stackalloc char[MobileLength];
        var count = 0;

        foreach (var character in input)
        {
            if (!char.IsAsciiDigit(character))
            {
                continue;
            }

            if (count == MobileLength)
            {
                return null;
            }

            digits[count++] = character;
        }

        return count is LandlineLength or MobileLength ? new string(digits[..count]) : null;
    }
}
