using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NaHora.Domain.ValueObjects;

namespace NaHora.Infrastructure.Persistence.ValueConverters;

/// <summary>
/// Persists <see cref="Cpf"/> as its unformatted string value.
/// </summary>
/// <remarks>
/// Reading back a value the database cannot parse means the stored data is
/// corrupt, which is exceptional rather than an expected outcome — so this
/// throws instead of returning a failed result.
/// </remarks>
internal sealed class CpfConverter : ValueConverter<Cpf, string>
{
    public CpfConverter()
        : base(
            value => value.Value,
            stored => Parse(stored))
    {
    }

    private static Cpf Parse(string stored) =>
        Cpf.TryParse(stored, out var value)
            ? value
            : throw new InvalidOperationException($"Stored Cpf is not valid: '{stored}'.");
}
