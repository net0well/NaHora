using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NaHora.Domain.ValueObjects;

namespace NaHora.Infrastructure.Persistence.ValueConverters;

/// <summary>
/// Persists an optional <see cref="EmailAddress"/> as its normalised value.
/// </summary>
internal sealed class NullableEmailAddressConverter : ValueConverter<EmailAddress?, string?>
{
    public NullableEmailAddressConverter()
        : base(
            value => value!.Value.Value,
            stored => Parse(stored!))
    {
    }

    private static EmailAddress? Parse(string stored) =>
        EmailAddress.TryParse(stored, out var value)
            ? value
            : throw new InvalidOperationException($"Stored EmailAddress is not valid: '{stored}'.");
}
