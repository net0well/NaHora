using System.Security.Cryptography;

namespace NaHora.IntegrationTests.Fixtures;

/// <summary>
/// Produces valid CPF and CNPJ values with correct check digits.
/// </summary>
/// <remarks>
/// Tests share one database, and both documents are unique. Hard-coding a
/// single valid pair would make every test after the first collide on the
/// unique index — and worse, make them depend on execution order.
///
/// The check digits are computed rather than guessed, because the value
/// objects reject anything else, which is exactly what they are for.
/// </remarks>
internal static class DocumentFactory
{
    public static string NewCpf()
    {
        var digits = new int[11];
        for (var index = 0; index < 9; index++)
        {
            digits[index] = RandomNumberGenerator.GetInt32(0, 10);
        }

        // Repeated digits pass the modulus but are rejected as never issued.
        if (digits.Take(9).Distinct().Count() == 1)
        {
            digits[0] = (digits[0] + 1) % 10;
        }

        digits[9] = CpfCheckDigit(digits, 9);
        digits[10] = CpfCheckDigit(digits, 10);

        return string.Concat(digits);
    }

    public static string NewCnpj()
    {
        var digits = new int[14];
        for (var index = 0; index < 12; index++)
        {
            digits[index] = RandomNumberGenerator.GetInt32(0, 10);
        }

        if (digits.Take(12).Distinct().Count() == 1)
        {
            digits[0] = (digits[0] + 1) % 10;
        }

        digits[12] = CnpjCheckDigit(digits, 12);
        digits[13] = CnpjCheckDigit(digits, 13);

        return string.Concat(digits);
    }

    private static int CpfCheckDigit(int[] digits, int length)
    {
        var sum = 0;
        var weight = length + 1;

        for (var index = 0; index < length; index++)
        {
            sum += digits[index] * weight--;
        }

        var remainder = sum % 11;

        return remainder < 2 ? 0 : 11 - remainder;
    }

    private static int CnpjCheckDigit(int[] digits, int length)
    {
        var sum = 0;
        var weight = 2;

        for (var index = length - 1; index >= 0; index--)
        {
            sum += digits[index] * weight;
            weight = weight == 9 ? 2 : weight + 1;
        }

        var remainder = sum % 11;

        return remainder < 2 ? 0 : 11 - remainder;
    }

    /// <summary>
    /// A PIS with a valid check digit.
    /// </summary>
    /// <remarks>
    /// Generated rather than fixed, so tests sharing one database stay
    /// independent of each other and of execution order.
    /// </remarks>
    public static string NewPis()
    {
        Span<int> weights = [3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        Span<char> digits = stackalloc char[11];

        var sum = 0;
        for (var index = 0; index < 10; index++)
        {
            var digit = Random.Shared.Next(0, 10);
            digits[index] = (char)('0' + digit);
            sum += digit * weights[index];
        }

        var remainder = sum % 11;
        digits[10] = (char)('0' + (remainder < 2 ? 0 : 11 - remainder));

        return new string(digits);
    }

    /// <summary>
    /// A short value that is actually distinct between two calls.
    /// </summary>
    /// <remarks>
    /// The tail, never the head. A version 7 GUID starts with a timestamp, so
    /// the first eight hex digits of two created in the same millisecond are
    /// identical — which turned "a unique registration" into a duplicate and a
    /// 409 that surfaced far from its cause.
    /// </remarks>
    public static string NewShortSuffix() => Guid.CreateVersion7().ToString("N")[^8..];
}
