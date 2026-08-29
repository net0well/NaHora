using FaceId.Domain.ValueObjects;

namespace FaceId.Domain.Companies;

/// <summary>
/// Confirms a CNPJ against the tax registry.
/// </summary>
public interface ICompanyRegistryLookup
{
    Task<CompanyRegistryResult> LookupAsync(Cnpj cnpj, CancellationToken cancellationToken = default);
}
