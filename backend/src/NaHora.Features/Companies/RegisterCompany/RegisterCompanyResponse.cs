namespace NaHora.Features.Companies.RegisterCompany;

/// <summary>
/// The provisioned tenant.
/// </summary>
/// <param name="CompanyId">
/// Needed to sign in: authentication is per company, so the app cannot
/// authenticate anybody without it.
/// </param>
/// <param name="RegistryVerified">
/// Whether the tax registry confirmed the CNPJ. False also means the registry
/// could not be reached — an outage on their side does not stop a company being
/// created, it only leaves the confirmation outstanding.
/// </param>
public sealed record RegisterCompanyResponse(
    Guid CompanyId,
    Guid AdministratorUserId,
    string Cnpj,
    string LegalName,
    string Status,
    bool RegistryVerified);
