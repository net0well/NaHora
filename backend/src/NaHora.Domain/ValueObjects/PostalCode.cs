using NaHora.Domain.Errors;
using NaHora.Shared.Results;

namespace NaHora.Domain.ValueObjects;

/// <summary>
/// Brazilian postal code (CEP).
/// </summary>
/// <remarks>
/// Eight digits, stored without punctuation so that a value typed as
/// <c>01310-100</c> and one typed as <c>01310100</c> are the same value and
/// compare equal. There is no check digit to verify — the only rule is the
/// shape.
/// </remarks>
public readonly record struct PostalCode
{
    private const int Length = 8;

    private PostalCode(string value) => Value = value;

    /// <summary>Eight digits, no punctuation.</summary>
    public string Value { get; }

    public static Result<PostalCode> Create(string? input, string? field = null) =>
        TryParse(input, out var postalCode)
            ? Result.Success(postalCode)
            : Result.Failure<PostalCode>(ValueObjectErrors.PostalCodeInvalid(field));

    public static bool TryParse(string? input, out PostalCode postalCode)
    {
        postalCode = default;

        var digits = Normalize(input);
        if (digits is null)
        {
            return false;
        }

        postalCode = new PostalCode(digits);

        return true;
    }

    /// <summary>"01310-100"</summary>
    public string Formatted => Value.Length == Length
        ? $"{Value[..5]}-{Value[5..]}"
        : Value;

    public override string ToString() => Value;

    private static string? Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        Span<char> digits = stackalloc char[Length];
        var count = 0;

        foreach (var character in input)
        {
            if (!char.IsAsciiDigit(character))
            {
                continue;
            }

            // More than eight digits is not a postal code with extra
            // punctuation; it is a different number.
            if (count == Length)
            {
                return null;
            }

            digits[count++] = character;
        }

        return count == Length ? new string(digits) : null;
    }
}
