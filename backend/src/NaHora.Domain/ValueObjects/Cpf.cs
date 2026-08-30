using System.Diagnostics.CodeAnalysis;

using NaHora.Domain.Errors;
using NaHora.Shared.Results;

namespace NaHora.Domain.ValueObjects;

/// <summary>
/// Brazilian individual taxpayer registry number.
/// </summary>
/// <remarks>
/// Stored unformatted. The type exists so a CPF can never travel as a loose
/// string: it is the login credential, the unique key within a company, and a
/// mandatory field of the AFD file, and an unvalidated one corrupts all three.
/// </remarks>
public readonly record struct Cpf
{
    private Cpf(string value) => Value = value;

    /// <summary>Eleven digits, no punctuation.</summary>
    public string Value { get; }

    /// <summary>
    /// Parses the input, reporting an invalid value as a failure rather than an
    /// exception: a mistyped document is an expected outcome of a form, not an
    /// exceptional condition.
    /// </summary>
    public static Result<Cpf> Create(string? input, string? field = null) =>
        TryParse(input, out var cpf)
            ? Result.Success(cpf)
            : Result.Failure<Cpf>(ValueObjectErrors.CpfInvalid(field));

    public static bool TryParse(string? input, out Cpf cpf)
    {
        cpf = default;

        var digits = Normalize(input);
        if (digits is null || !IsCheckDigitValid(digits))
        {
            return false;
        }

        cpf = new Cpf(digits);
        return true;
    }

    private static string? Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        Span<char> buffer = stackalloc char[11];
        var count = 0;

        foreach (var character in input)
        {
            if (!char.IsAsciiDigit(character))
            {
                continue;
            }

            if (count == 11)
            {
                return null;
            }

            buffer[count++] = character;
        }

        return count == 11 ? new string(buffer) : null;
    }

    private static bool IsCheckDigitValid(string digits)
    {
        // Repeated digits satisfy the modulus but are not issued. Rejecting them
        // explicitly is what stops 111.111.111-11 from passing as valid.
        if (AllDigitsEqual(digits))
        {
            return false;
        }

        return ComputeCheckDigit(digits, 9) == digits[9] - '0'
               && ComputeCheckDigit(digits, 10) == digits[10] - '0';
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

    private static int ComputeCheckDigit(string digits, int length)
    {
        var sum = 0;
        var weight = length + 1;

        for (var index = 0; index < length; index++)
        {
            sum += (digits[index] - '0') * weight--;
        }

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }

    /// <summary>Formatted for display: <c>123.456.789-09</c>.</summary>
    public string ToFormattedString() =>
        $"{Value[..3]}.{Value[3..6]}.{Value[6..9]}-{Value[9..]}";

    /// <summary>
    /// Masked for audit trails and logs, where the record must identify whom it
    /// concerns without republishing the full document.
    /// </summary>
    public string ToMaskedString() => $"***.{Value[3..6]}.{Value[6..9]}-**";

    public override string ToString() => Value;

    public static implicit operator string(Cpf cpf) => cpf.Value;
}
