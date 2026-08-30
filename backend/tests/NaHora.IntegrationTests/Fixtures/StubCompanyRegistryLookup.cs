using NaHora.Domain.Companies;
using NaHora.Domain.ValueObjects;

namespace NaHora.IntegrationTests.Fixtures;

/// <summary>
/// Stands in for the tax registry.
/// </summary>
/// <remarks>
/// The real adapter calls BrasilAPI over the network, and the CNPJs these tests
/// generate are valid in shape but belong to nobody — so a runner with internet
/// gets a conclusive "does not exist" and every registration test fails, while a
/// runner without it gets "unavailable" and they all pass. That difference is
/// not something these tests are about.
///
/// What the real adapter does with a real answer belongs to a test of the
/// adapter, not to every test that happens to create a company.
///
/// Public and with a parameterless constructor so Wolverine's generated handler
/// code can build it, like the type it replaces.
/// </remarks>
public sealed class StubCompanyRegistryLookup : ICompanyRegistryLookup
{
    /// <summary>
    /// What the next lookup answers. Unavailable by default: it is the outcome
    /// that must not block a sign-up, so it is the one worth exercising most.
    /// </summary>
    public static CompanyRegistryResult NextResult { get; set; } = CompanyRegistryResult.Unavailable();

    public Task<CompanyRegistryResult> LookupAsync(
        Cnpj cnpj,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(NextResult);
}
