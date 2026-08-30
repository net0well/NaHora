using System.Collections.Frozen;
using NaHora.Domain.Errors;
using NaHora.Shared.Results;

namespace NaHora.Domain.ValueObjects;

/// <summary>
/// A Brazilian postal address.
/// </summary>
/// <remarks>
/// <para>
/// Shared by the company and the employee. One shape rather than two means an
/// address renders and validates the same way wherever it appears, and a fix to
/// the rules lands in both places at once.
/// </para>
/// <para>
/// This is not a punch location. It is where an establishment or a person is
/// registered, typed by a human — it carries no coordinates, no accuracy, and
/// never feeds the attendance record.
/// </para>
/// </remarks>
public sealed record Address
{
    /// <summary>The 26 states plus the Federal District.</summary>
    private static readonly FrozenSet<string> States = new[]
    {
        "AC", "AL", "AP", "AM", "BA", "CE", "DF", "ES", "GO", "MA", "MT", "MS",
        "MG", "PA", "PB", "PR", "PE", "PI", "RJ", "RN", "RS", "RO", "RR", "SC",
        "SP", "SE", "TO"
    }.ToFrozenSet(StringComparer.Ordinal);

    private Address(
        PostalCode postalCode,
        string street,
        string number,
        string? complement,
        string? district,
        string city,
        string state)
    {
        PostalCode = postalCode;
        Street = street;
        Number = number;
        Complement = complement;
        District = district;
        City = city;
        State = state;
    }

    public PostalCode PostalCode { get; }

    /// <summary>Logradouro — "Avenida Paulista".</summary>
    public string Street { get; }

    /// <summary>
    /// Kept as text, not a number.
    /// </summary>
    /// <remarks>
    /// Real addresses carry "S/N", "1000-A" and "Km 12". Storing an integer
    /// would make those unrepresentable, and the workaround is always a second
    /// field nobody fills in.
    /// </remarks>
    public string Number { get; }

    /// <summary>Complemento — "Sala 402".</summary>
    public string? Complement { get; }

    /// <summary>Bairro.</summary>
    public string? District { get; }

    public string City { get; }

    /// <summary>Two-letter state code, upper case.</summary>
    public string State { get; }

    /// <summary>
    /// Builds an address, collecting every problem before returning.
    /// </summary>
    /// <remarks>
    /// District and complement are optional because plenty of real addresses
    /// have neither, and a required field somebody has to invent a value for is
    /// worse than an absent one.
    /// </remarks>
    public static Result<Address> Create(
        string? postalCode,
        string? street,
        string? number,
        string? complement,
        string? district,
        string? city,
        string? state)
    {
        var errors = new List<Error>();

        var parsedPostalCode = PostalCode.Create(postalCode, "postalCode");
        if (parsedPostalCode.IsFailure)
        {
            errors.AddRange(parsedPostalCode.Errors);
        }

        if (string.IsNullOrWhiteSpace(street))
        {
            errors.Add(ValueObjectErrors.AddressStreetRequired("street"));
        }

        if (string.IsNullOrWhiteSpace(number))
        {
            errors.Add(ValueObjectErrors.AddressNumberRequired("number"));
        }

        if (string.IsNullOrWhiteSpace(city))
        {
            errors.Add(ValueObjectErrors.AddressCityRequired("city"));
        }

        var normalisedState = state?.Trim().ToUpperInvariant();
        if (normalisedState is null || !States.Contains(normalisedState))
        {
            errors.Add(ValueObjectErrors.AddressStateInvalid("state"));
        }

        if (errors.Count > 0)
        {
            return Result.Failure<Address>(errors);
        }

        return new Address(
            parsedPostalCode.Value,
            street!.Trim(),
            number!.Trim(),
            NullIfBlank(complement),
            NullIfBlank(district),
            city!.Trim(),
            normalisedState!);
    }

    /// <summary>One line, the way it would be written on an envelope.</summary>
    public string SingleLine
    {
        get
        {
            var head = $"{Street}, {Number}";

            if (Complement is not null)
            {
                head += $" - {Complement}";
            }

            if (District is not null)
            {
                head += $" - {District}";
            }

            return $"{head} - {City}/{State} - {PostalCode.Formatted}";
        }
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
