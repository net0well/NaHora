using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NaHora.Domain.ValueObjects;

namespace NaHora.Infrastructure.Persistence.ValueConverters;

/// <summary>
/// Persists <see cref="PostalCode"/> as its unformatted digits.
/// </summary>
internal sealed class PostalCodeConverter : ValueConverter<PostalCode, string>
{
    public PostalCodeConverter()
        : base(
            value => value.Value,
            stored => Parse(stored))
    {
    }

    private static PostalCode Parse(string stored) =>
        PostalCode.TryParse(stored, out var value)
            ? value
            : throw new InvalidOperationException($"Stored PostalCode is not valid: '{stored}'.");
}
