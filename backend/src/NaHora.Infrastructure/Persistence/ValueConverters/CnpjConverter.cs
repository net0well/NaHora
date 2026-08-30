using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NaHora.Domain.ValueObjects;

namespace NaHora.Infrastructure.Persistence.ValueConverters;

/// <summary>
/// Persists <see cref="Cnpj"/> as its unformatted string value.
/// </summary>
/// <remarks>
/// Reading back a value the database cannot parse means the stored data is
/// corrupt, which is exceptional rather than an expected outcome — so this
/// throws instead of returning a failed result.
/// </remarks>
internal sealed class CnpjConverter : ValueConverter<Cnpj, string>
{
    public CnpjConverter()
        : base(
            value => value.Value,
            stored => Parse(stored))
    {
    }

    private static Cnpj Parse(string stored) =>
        Cnpj.TryParse(stored, out var value)
            ? value
            : throw new InvalidOperationException($"Stored Cnpj is not valid: '{stored}'.");
}
