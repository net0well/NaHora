using NaHora.Domain.Abstractions;
using NaHora.Domain.Companies;
using NaHora.Domain.Errors;
using NaHora.Features.Common;
using NaHora.Shared.Results;

namespace NaHora.Features.Companies.GetMyCompany;

/// <summary>
/// The company the caller works for.
/// </summary>
/// <remarks>
/// Open to any authenticated user, not only managers. An employee is entitled
/// to know which legal entity is recording their hours — it is the one named on
/// their own time record.
/// </remarks>
public static class GetMyCompanyHandler
{
    public static async Task<Result<GetMyCompanyResponse>> Handle(
        GetMyCompanyQuery query,
        ICurrentUser currentUser,
        ICompanyRepository companies,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(currentUser);
        ArgumentNullException.ThrowIfNull(companies);

        // From the token, never from the request. The whole isolation model
        // rests on the caller being unable to name a different company.
        var company = await companies.GetByIdAsync(currentUser.CompanyId, cancellationToken);

        if (company is null)
        {
            return Result.Failure<GetMyCompanyResponse>(CompanyErrors.NotFound());
        }

        return Result.Success(new GetMyCompanyResponse(
            company.Id,
            company.Cnpj.Value,
            company.Cnpj.Formatted,
            company.LegalName,
            company.TradeName,
            company.Email.Value,
            company.Phone?.Value,
            company.Phone?.Formatted,
            company.StateRegistration,
            company.MunicipalRegistration,
            company.FoundedOn,
            company.TimeZoneId,
            company.Status.ToString(),
            company.RegistryVerified,
            AddressResponse.From(company.Address)));
    }
}
