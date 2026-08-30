using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NaHora.Domain.ValueObjects;

namespace NaHora.Infrastructure.Persistence.ValueConverters;

/// <summary>
/// Persists an optional <see cref="Pis"/>. Null stays null: an employee without
/// a PIS is a valid record that simply cannot enter the AFD export.
/// </summary>
internal sealed class NullablePisConverter : ValueConverter<Pis?, string?>
{
    public NullablePisConverter()
        : base(
            value => value == null ? null : value.Value.Value,
            stored => Parse(stored))
    {
    }

    private static Pis? Parse(string? stored)
    {
        if (stored is null)
        {
            return null;
        }

        return Pis.TryParse(stored, out var value)
            ? value
            : throw new InvalidOperationException($"Stored PIS is not valid: '{stored}'.");
    }
}
