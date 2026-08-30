namespace NaHora.Features.Companies.RegisterCompany;

/// <summary>A postal address as it arrives on the wire.</summary>
public sealed record AddressInput(
    string? PostalCode,
    string? Street,
    string? Number,
    string? Complement,
    string? District,
    string? City,
    string? State);
