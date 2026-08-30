namespace NaHora.Features.Companies.RegisterCompany;

/// <summary>
/// Provisions a new tenant: the company and the administrator who can sign into
/// it.
/// </summary>
/// <param name="TimeZoneId">
/// Null falls back to São Paulo. Brazil spans four zones, and assuming one
/// would misreport lateness for everybody outside it — but asking an operator
/// to type an IANA identifier for the common case is friction for nothing.
/// </param>
public sealed record RegisterCompanyCommand(
    string? Cnpj,
    string? LegalName,
    string? TradeName,
    string? Email,
    string? Phone,
    string? StateRegistration,
    string? MunicipalRegistration,
    DateOnly? FoundedOn,
    string? TimeZoneId,
    AddressInput? Address,
    CompanyAdministratorInput? Administrator);
