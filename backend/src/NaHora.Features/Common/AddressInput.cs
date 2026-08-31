namespace NaHora.Features.Common;

/// <summary>
/// A postal address as it arrives on the wire.
/// </summary>
/// <remarks>
/// In Common because a company and an employee both carry one, and slices do
/// not reference each other.
/// </remarks>
public sealed record AddressInput(
    string? PostalCode,
    string? Street,
    string? Number,
    string? Complement,
    string? District,
    string? City,
    string? State);
