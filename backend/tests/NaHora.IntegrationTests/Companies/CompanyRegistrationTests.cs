using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using NaHora.Domain.Companies;
using NaHora.IntegrationTests.Fixtures;

namespace NaHora.IntegrationTests.Companies;

/// <summary>
/// Provisioning a tenant through the API.
/// </summary>
/// <remarks>
/// Exercised over the real pipeline because the handler is built by Wolverine's
/// code generation, and whether it *can* be built is only discovered when a
/// message actually arrives. A unit test on the handler would have passed while
/// every real request returned 500.
/// </remarks>
public sealed class CompanyRegistrationTests(ApiFixture fixture) : IClassFixture<ApiFixture>, IDisposable
{
    private readonly HttpClient _client = fixture.CreateClient();

    /// <summary>Leaves the registry answering what the other tests expect.</summary>
    public void Dispose() =>
        StubCompanyRegistryLookup.NextResult = CompanyRegistryResult.Unavailable();

    [Fact]
    public async Task Register_WithACompletePayload_CreatesTheCompanyAndItsAdministrator()
    {
        // Arrange — the registry confirms it
        StubCompanyRegistryLookup.NextResult =
            CompanyRegistryResult.Found("Segunda Empresa Comercio LTDA", "Segunda", isActive: true);

        var cnpj = DocumentFactory.NewCnpj();
        var cpf = DocumentFactory.NewCpf();

        // Act
        var response = await PostAsync(NewBody(cnpj, cpf));

        // Assert
        var raw = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.Created, raw);

        var body = JsonDocument.Parse(raw).RootElement;
        body.GetProperty("companyId").GetGuid().ShouldNotBe(Guid.Empty);
        body.GetProperty("administratorUserId").GetGuid().ShouldNotBe(Guid.Empty);
        body.GetProperty("cnpj").GetString().ShouldBe(cnpj);

        // Active rather than pending: this is an operator action through the
        // API, and there is nobody to confirm an address to.
        body.GetProperty("status").GetString().ShouldBe("Active");
        body.GetProperty("registryVerified").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    public async Task Register_WhenTheRegistrySaysTheCnpjDoesNotExist_IsRefused()
    {
        // Arrange — conclusive, so retrying would not change the answer
        StubCompanyRegistryLookup.NextResult = CompanyRegistryResult.NotFound();

        // Act
        var response = await PostAsync(NewBody(DocumentFactory.NewCnpj(), DocumentFactory.NewCpf()));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .ShouldContain("CNPJ_NOT_FOUND_IN_REGISTRY");
    }

    [Fact]
    public async Task Register_WhenTheRegistryCannotBeReached_ProceedsUnverified()
    {
        // Arrange — an outage on their side must not stop a customer signing
        // up, and it must not be recorded as a verification either
        StubCompanyRegistryLookup.NextResult = CompanyRegistryResult.Unavailable();

        // Act
        var response = await PostAsync(NewBody(DocumentFactory.NewCnpj(), DocumentFactory.NewCpf()));

        // Assert
        var raw = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.Created, raw);
        JsonDocument.Parse(raw).RootElement
            .GetProperty("registryVerified").GetBoolean()
            .ShouldBeFalse("unreachable is not the same as confirmed");
    }

    [Fact]
    public async Task Register_ThenSignIn_WorksForTheNewAdministrator()
    {
        // Arrange — a tenant nobody can sign into is a row nobody can reach
        var cpf = DocumentFactory.NewCpf();
        var created = await PostAsync(NewBody(DocumentFactory.NewCnpj(), cpf));
        var companyId = JsonDocument
            .Parse(await created.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .RootElement.GetProperty("companyId").GetGuid();

        // Act
        var login = await _client.PostAsJsonAsync(
            new Uri("/api/v1/auth/login", UriKind.Relative),
            new { cpf, password = AdministratorPassword, companyId },
            TestContext.Current.CancellationToken);

        // Assert
        var raw = await login.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        login.StatusCode.ShouldBe(HttpStatusCode.OK, raw);

        var body = JsonDocument.Parse(raw).RootElement;
        body.GetProperty("accessToken").GetString().ShouldNotBeNullOrWhiteSpace();

        // The operator typed this password, not its owner
        body.GetProperty("pendingActions").EnumerateArray()
            .Select(action => action.GetString())
            .ShouldContain("ChangePassword");
    }

    [Fact]
    public async Task Register_WithTheSameCnpjTwice_IsRefused()
    {
        // Arrange — one CNPJ is one legal entity, and a second tenant for it
        // would split that company's records in two
        var cnpj = DocumentFactory.NewCnpj();
        await PostAsync(NewBody(cnpj, DocumentFactory.NewCpf()));

        // Act
        var second = await PostAsync(NewBody(cnpj, DocumentFactory.NewCpf()));

        // Assert
        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await second.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .ShouldContain("CNPJ_ALREADY_REGISTERED");
    }

    [Fact]
    public async Task Register_WithSeveralBadFields_ReportsAllOfThem()
    {
        // Arrange — a form that rejects one field per round trip is a form
        // people abandon
        var body = NewBody(DocumentFactory.NewCnpj(), DocumentFactory.NewCpf());
        body["cnpj"] = "123";
        ((Dictionary<string, object?>)body["address"]!)["postalCode"] = "1";
        ((Dictionary<string, object?>)body["address"]!)["state"] = "XX";

        // Act
        var response = await PostAsync(body);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);

        var raw = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        raw.ShouldContain("CNPJ_INVALID");
        raw.ShouldContain("POSTAL_CODE_INVALID");
        raw.ShouldContain("ADDRESS_STATE_INVALID");
    }

    [Fact]
    public async Task Register_WithAPasswordMadeOfTheirOwnDetails_IsRefused()
    {
        // Arrange — checked before it is ever hashed
        var cpf = DocumentFactory.NewCpf();
        var body = NewBody(DocumentFactory.NewCnpj(), cpf);
        ((Dictionary<string, object?>)body["administrator"]!)["password"] = cpf;

        // Act
        var response = await PostAsync(body);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .ShouldContain("PASSWORD");
    }

    [Fact]
    public async Task Register_WithoutAnAddress_IsRefused()
    {
        // Arrange — a company without an establishment address is not a company
        var body = NewBody(DocumentFactory.NewCnpj(), DocumentFactory.NewCpf());
        body["address"] = null;

        // Act
        var response = await PostAsync(body);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .ShouldContain("COMPANY_ADDRESS_REQUIRED");
    }

    private const string AdministratorPassword = "Provisionamento#2026";

    private Task<HttpResponseMessage> PostAsync(Dictionary<string, object?> body) =>
        _client.PostAsJsonAsync(
            new Uri("/api/v1/companies", UriKind.Relative),
            body,
            TestContext.Current.CancellationToken);

    private static Dictionary<string, object?> NewBody(string cnpj, string cpf) =>
        new(StringComparer.Ordinal)
        {
            ["cnpj"] = cnpj,
            ["legalName"] = "Segunda Empresa Comercio LTDA",
            ["tradeName"] = "Segunda",
            ["email"] = $"contato-{Guid.CreateVersion7():N}@segunda.com.br",
            ["phone"] = "(11) 4002-8922",
            ["stateRegistration"] = "123.456.789.111",
            ["foundedOn"] = "2019-03-15",
            ["address"] = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["postalCode"] = "04538-133",
                ["street"] = "Avenida Brigadeiro Faria Lima",
                ["number"] = "3477",
                ["complement"] = "14o andar",
                ["district"] = "Itaim Bibi",
                ["city"] = "São Paulo",
                ["state"] = "SP"
            },
            ["administrator"] = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["cpf"] = cpf,
                ["fullName"] = "Carlos Administrador",
                ["email"] = $"admin-{Guid.CreateVersion7():N}@segunda.com.br",
                ["password"] = AdministratorPassword
            }
        };
}
