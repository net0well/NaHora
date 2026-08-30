namespace NaHora.Features.Companies.GetMyCompany;

/// <summary>
/// The caller's own company.
/// </summary>
/// <remarks>
/// Carries no identifier. The company comes from the token, never from the
/// request — if a caller could name it, tenant isolation would be decorative.
/// </remarks>
public sealed record GetMyCompanyQuery;
