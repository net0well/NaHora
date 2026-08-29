using FaceId.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FaceId.Infrastructure.Persistence.ValueConverters;

/// <summary>
/// Persists <see cref="EmailAddress"/> as its unformatted string value.
/// </summary>
/// <remarks>
/// Reading back a value the database cannot parse means the stored data is
/// corrupt, which is exceptional rather than an expected outcome — so this
/// throws instead of returning a failed result.
/// </remarks>
internal sealed class EmailAddressConverter : ValueConverter<EmailAddress, string>
{
    public EmailAddressConverter()
        : base(
            value => value.Value,
            stored => Parse(stored))
    {
    }

    private static EmailAddress Parse(string stored) =>
        EmailAddress.TryParse(stored, out var value)
            ? value
            : throw new InvalidOperationException($"Stored EmailAddress is not valid: '{stored}'.");
}
