using NaHora.Features.Common;

namespace NaHora.Features.Companies.GetMyCompany;

/// <summary>
/// The company the caller works for.
/// </summary>
/// <remarks>
/// Documents travel formatted, because every screen that shows a CNPJ shows it
/// punctuated and doing it in one place beats doing it in each of them.
/// </remarks>
public sealed record GetMyCompanyResponse(
    Guid Id,
    string Cnpj,
    string CnpjFormatted,
    string LegalName,
    string? TradeName,
    string Email,
    string? Phone,
    string? PhoneFormatted,
    string? StateRegistration,
    string? MunicipalRegistration,
    DateOnly? FoundedOn,
    string TimeZoneId,
    string Status,
    bool RegistryVerified,
    AddressResponse Address);
