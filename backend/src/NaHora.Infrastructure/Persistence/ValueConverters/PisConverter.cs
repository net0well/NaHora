using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NaHora.Domain.ValueObjects;

namespace NaHora.Infrastructure.Persistence.ValueConverters;

/// <summary>
/// Persists <see cref="Pis"/> as its unformatted string value.
/// </summary>
/// <remarks>
/// Reading back a value the database cannot parse means the stored data is
/// corrupt, which is exceptional rather than an expected outcome — so this
/// throws instead of returning a failed result.
/// </remarks>
internal sealed class PisConverter : ValueConverter<Pis, string>
{
    public PisConverter()
        : base(
            value => value.Value,
            stored => Parse(stored))
    {
    }

    private static Pis Parse(string stored) =>
        Pis.TryParse(stored, out var value)
            ? value
            : throw new InvalidOperationException($"Stored Pis is not valid: '{stored}'.");
}
