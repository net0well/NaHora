using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NaHora.Domain.ValueObjects;

namespace NaHora.Infrastructure.Persistence.ValueConverters;

/// <summary>
/// Persists an optional <see cref="PhoneNumber"/> as its unformatted digits.
/// </summary>
/// <remarks>
/// EF applies a converter only to non-null values, so the null case never
/// reaches the expressions below.
/// </remarks>
internal sealed class NullablePhoneNumberConverter : ValueConverter<PhoneNumber?, string?>
{
    public NullablePhoneNumberConverter()
        : base(
            value => value!.Value.Value,
            stored => Parse(stored!))
    {
    }

    private static PhoneNumber? Parse(string stored) =>
        PhoneNumber.TryParse(stored, out var value)
            ? value
            : throw new InvalidOperationException($"Stored PhoneNumber is not valid: '{stored}'.");
}
