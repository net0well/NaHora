namespace FaceId.Domain.Companies;

/// <summary>
/// Outcome of a registry lookup.
/// </summary>
/// <remarks>
/// The distinction between <see cref="RegistryLookupOutcome.NotFound"/> and
/// <see cref="RegistryLookupOutcome.Unavailable"/> is the whole point of this
/// type, and it is the difference between a conclusive answer and no answer at
/// all. A CNPJ the registry says does not exist blocks the sign-up. A registry
/// we could not reach must not.
/// </remarks>
public sealed record CompanyRegistryResult(
    RegistryLookupOutcome Outcome,
    string? LegalName = null,
    string? TradeName = null,
    bool IsActive = false)
{
    public static CompanyRegistryResult Found(string legalName, string? tradeName, bool isActive) =>
        new(RegistryLookupOutcome.Found, legalName, tradeName, isActive);

    public static CompanyRegistryResult NotFound() => new(RegistryLookupOutcome.NotFound);

    public static CompanyRegistryResult Unavailable() => new(RegistryLookupOutcome.Unavailable);
}
