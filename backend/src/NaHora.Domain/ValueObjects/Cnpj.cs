using NaHora.Domain.Errors;
using NaHora.Shared.Results;

namespace NaHora.Domain.ValueObjects;

/// <summary>
/// Brazilian company registry number.
/// </summary>
/// <remarks>
/// Supports the alphanumeric format in force since 2026 alongside the classic
/// numeric one. In the alphanumeric layout the first twelve positions may be
/// letters or digits and the two check digits remain numeric; the modulus 11
/// calculation uses each character's ASCII value minus 48, which reduces to the
/// familiar arithmetic for digits.
/// </remarks>
public readonly record struct Cnpj
{
    private const int Length = 14;
    private const int BaseLength = 12;

    private Cnpj(string value) => Value = value;

    /// <summary>Fourteen characters, no punctuation, upper case.</summary>
    public string Value { get; }

    /// <summary>
    /// Parses the input, reporting an invalid value as a failure rather than an
    /// exception: a mistyped document is an expected outcome of a form, not an
    /// exceptional condition.
    /// </summary>
    public static Result<Cnpj> Create(string? input, string? field = null) =>
        TryParse(input, out var cnpj)
            ? Result.Success(cnpj)
            : Result.Failure<Cnpj>(ValueObjectErrors.CnpjInvalid(field));

    public static bool TryParse(string? input, out Cnpj cnpj)
    {
        cnpj = default;

        var normalized = Normalize(input);
        if (normalized is null || !IsCheckDigitValid(normalized))
        {
            return false;
        }

        cnpj = new Cnpj(normalized);
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
            if (!char.IsAsciiLetterOrDigit(character))
            {
                continue;
            }

            if (count == Length)
            {
                return null;
            }

            buffer[count++] = char.ToUpperInvariant(character);
        }

        if (count != Length)
        {
            return null;
        }

        // The check digits are always numeric, in both formats.
        if (!char.IsAsciiDigit(buffer[12]) || !char.IsAsciiDigit(buffer[13]))
        {
            return null;
        }

        return new string(buffer);
    }

    private static bool IsCheckDigitValid(string value)
    {
        if (AllCharactersEqual(value))
        {
            return false;
        }

        return ComputeCheckDigit(value, BaseLength) == value[12] - '0'
               && ComputeCheckDigit(value, BaseLength + 1) == value[13] - '0';
    }

    private static bool AllCharactersEqual(string value)
    {
        for (var index = 1; index < value.Length; index++)
        {
            if (value[index] != value[0])
            {
                return false;
            }
        }

        return true;
    }

    private static int ComputeCheckDigit(string value, int length)
    {
        var sum = 0;
        var weight = 2;

        for (var index = length - 1; index >= 0; index--)
        {
            sum += (value[index] - '0') * weight;
            weight = weight == 9 ? 2 : weight + 1;
        }

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }

    /// <summary>Formatted for display: <c>12.345.678/0001-99</c>.</summary>
    public string ToFormattedString() =>
        $"{Value[..2]}.{Value[2..5]}.{Value[5..8]}/{Value[8..12]}-{Value[12..]}";

    public override string ToString() => Value;

    public static implicit operator string(Cnpj cnpj) => cnpj.Value;
}
