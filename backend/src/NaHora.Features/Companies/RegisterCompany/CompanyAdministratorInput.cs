namespace NaHora.Features.Companies.RegisterCompany;

/// <summary>
/// The first account of a new company.
/// </summary>
/// <remarks>
/// Created in the same transaction as the company. A tenant with no way to sign
/// in is a row nobody can reach, and creating the account afterwards would need
/// an authenticated caller that does not exist yet.
/// </remarks>
public sealed record CompanyAdministratorInput(
    string? Cpf,
    string? FullName,
    string? Email,
    string? Password);
