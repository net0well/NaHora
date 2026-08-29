using FaceId.Domain.Errors;
using FaceId.Shared.Results;

namespace FaceId.Domain.ValueObjects;

/// <summary>
/// PIS/PASEP/NIT number.
/// </summary>
/// <remarks>
/// Required by the AFD record that identifies a worker, so an employee without
/// a valid PIS cannot appear in the regulatory export. The type is optional on
/// <c>Employee</c> precisely because the absence must be reported as a warning
/// rather than block a hire.
/// </remarks>
public readonly record struct Pis
{
    private const int Length = 11;

    // Modulus 11 weights defined by the PIS specification, applied left to right.
    private static readonly int[] Weights = [3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

    private Pis(string value) => Value = value;

    /// <summary>Eleven digits, no punctuation.</summary>
    public string Value { get; }

    /// <summary>
    /// Parses the input, reporting an invalid value as a failure rather than an
    /// exception: a mistyped document is an expected outcome of a form, not an
    /// exceptional condition.
    /// </summary>
    public static Result<Pis> Create(string? input, string? field = null) =>
        TryParse(input, out var pis)
            ? Result.Success(pis)
            : Result.Failure<Pis>(ValueObjectErrors.PisInvalid(field));

    public static bool TryParse(string? input, out Pis pis)
    {
        pis = default;

        var digits = Normalize(input);
        if (digits is null || !IsCheckDigitValid(digits))
        {
            return false;
        }

        pis = new Pis(digits);
        return true;
    }

    private static string? Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        Span<char> buffer = stackalloc char[Length];
        var count = 0;

        foreach (var character in input)
        {
            if (!char.IsAsciiDigit(character))
            {
                continue;
            }

            if (count == Length)
            {
                return null;
            }

            buffer[count++] = character;
        }

        return count == Length ? new string(buffer) : null;
    }

    private static bool IsCheckDigitValid(string digits)
    {
        if (AllDigitsEqual(digits))
        {
            return false;
        }

        var sum = 0;
        for (var index = 0; index < Weights.Length; index++)
        {
            sum += (digits[index] - '0') * Weights[index];
        }

        var remainder = sum % 11;
        var expected = remainder < 2 ? 0 : 11 - remainder;

        return expected == digits[10] - '0';
    }

    private static bool AllDigitsEqual(string digits)
    {
        for (var index = 1; index < digits.Length; index++)
        {
            if (digits[index] != digits[0])
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Formatted for display: <c>123.45678.90-1</c>.</summary>
    public string ToFormattedString() =>
        $"{Value[..3]}.{Value[3..8]}.{Value[8..10]}-{Value[10..]}";

    public override string ToString() => Value;

    public static implicit operator string(Pis pis) => pis.Value;
}
