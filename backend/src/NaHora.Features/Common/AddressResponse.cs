using NaHora.Domain.ValueObjects;

namespace NaHora.Features.Common;

/// <summary>
/// A postal address on the wire.
/// </summary>
/// <remarks>
/// Lives in Common rather than in a slice because both the company and the
/// employee return one, and slices do not reference each other. Duplicating it
/// would let the two drift into different shapes for the same value.
///
/// The assembled line travels alongside the parts rather than instead of them:
/// a narrow screen renders one line, and anyone editing the address needs the
/// fields separately.
/// </remarks>
public sealed record AddressResponse(
    string PostalCode,
    string Street,
    string Number,
    string? Complement,
    string? District,
    string City,
    string State,
    string SingleLine)
{
    public static AddressResponse From(Address address)
    {
        ArgumentNullException.ThrowIfNull(address);

        return new AddressResponse(
            address.PostalCode.Formatted,
            address.Street,
            address.Number,
            address.Complement,
            address.District,
            address.City,
            address.State,
            address.SingleLine);
    }
}
