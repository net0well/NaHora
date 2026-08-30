using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using NaHora.Domain.Companies;
using NaHora.Domain.ValueObjects;

namespace NaHora.Infrastructure.Registry;

/// <summary>
/// Looks a CNPJ up in the Brazilian tax registry through BrasilAPI.
/// </summary>
/// <remarks>
/// Takes the factory rather than an <see cref="HttpClient"/> so that Wolverine's
/// generated handler code can construct it directly. A typed client is
/// registered through a factory lambda, and Wolverine refuses to generate a
/// handler that would have to locate a service from the container — which it
/// only discovers at the moment the first message arrives, long after startup.
///
/// The resilience pipeline is attached at registration, not here, so this class
/// only has to answer one question: was the answer conclusive?
/// </remarks>
public sealed class BrasilApiCompanyRegistryLookup(
    IHttpClientFactory httpClientFactory,
    ILogger<BrasilApiCompanyRegistryLookup> logger) : ICompanyRegistryLookup
{
    /// <summary>Names the configured client, shared with the registration.</summary>
    public const string HttpClientName = "company-registry";

    public async Task<CompanyRegistryResult> LookupAsync(
        Cnpj cnpj,
        CancellationToken cancellationToken = default)
    {
        var httpClient = httpClientFactory.CreateClient(HttpClientName);

        try
        {
            using var response = await httpClient.GetAsync(
                new Uri($"cnpj/v1/{cnpj.Value}", UriKind.Relative),
                cancellationToken);

            // Conclusive: the registry says this CNPJ does not exist. Retrying
            // would not change the answer, and the sign-up must be blocked.
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return CompanyRegistryResult.NotFound();
            }

            if (!response.IsSuccessStatusCode)
            {
                RegistryLog.LookupUnavailable(logger, (int)response.StatusCode);

                return CompanyRegistryResult.Unavailable();
            }

            var payload = await response.Content.ReadFromJsonAsync<BrasilApiCompany>(cancellationToken);

            if (payload?.LegalName is null)
            {
                return CompanyRegistryResult.Unavailable();
            }

            return CompanyRegistryResult.Found(
                payload.LegalName,
                payload.TradeName,

                // 2 is "ativa" in the registry's own coding.
                isActive: payload.RegistrationStatusCode == 2);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            // Includes the open circuit and the exhausted timeout. Absence of an
            // answer degrades the record; it never blocks the customer.
            RegistryLog.LookupFailed(logger, exception);

            return CompanyRegistryResult.Unavailable();
        }
    }

    private sealed record BrasilApiCompany
    {
        [JsonPropertyName("razao_social")]
        public string? LegalName { get; init; }

        [JsonPropertyName("nome_fantasia")]
        public string? TradeName { get; init; }

        [JsonPropertyName("descricao_situacao_cadastral")]
        public string? RegistrationStatus { get; init; }

        [JsonPropertyName("situacao_cadastral")]
        public int RegistrationStatusCode { get; init; }
    }
}
