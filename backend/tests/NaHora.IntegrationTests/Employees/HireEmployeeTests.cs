using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using NaHora.Domain.Users;
using NaHora.Infrastructure.Persistence;
using NaHora.IntegrationTests.Fixtures;

namespace NaHora.IntegrationTests.Employees;

/// <summary>
/// Registering an employee, over the real pipeline.
/// </summary>
/// <remarks>
/// Exercised end to end because the handler is built by Wolverine's code
/// generation, and whether it can be built at all is only discovered when a
/// message actually arrives.
/// </remarks>
public sealed class HireEmployeeTests(ApiFixture fixture) : IClassFixture<ApiFixture>, IDisposable
{
    private readonly HttpClient _client = fixture.CreateClient();

    public void Dispose() => StubEmailSender.ResetDelivery();

    [Fact]
    public async Task Hire_CreatesTheEmploymentRecordAndTheAccount()
    {
        // Arrange
        var manager = await SignedInManagerAsync();
        var cpf = DocumentFactory.NewCpf();

        // Act
        var response = await PostAsync(manager, NewBody(cpf));

        // Assert
        var raw = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.Created, raw);

        var body = JsonDocument.Parse(raw).RootElement;
        body.GetProperty("employeeId").GetGuid().ShouldNotBe(Guid.Empty);
        body.GetProperty("userId").GetGuid().ShouldNotBe(Guid.Empty);
        body.GetProperty("welcomeEmailDelivered").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    public async Task Hire_NeverReturnsThePasswordToWhoeverHired()
    {
        // Arrange — a manager who learns the credential of the person whose
        // hours they audit is the one leak this flow exists to avoid
        var manager = await SignedInManagerAsync();

        // Act
        var response = await PostAsync(manager, NewBody(DocumentFactory.NewCpf()));

        // Assert
        var raw = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        raw.ShouldNotContain("password", Case.Insensitive);
        raw.ShouldNotContain("senha", Case.Insensitive);
    }

    [Fact]
    public async Task Hire_EmailsThePasswordToTheEmployee_AndTheyCanSignInWithIt()
    {
        // Arrange
        var manager = await SignedInManagerAsync();
        var cpf = DocumentFactory.NewCpf();
        var body = NewBody(cpf);
        var employeeEmail = (string)body["email"]!;

        // Act
        await PostAsync(manager, body);

        var message = StubEmailSender.LastTo(employeeEmail);
        message.ShouldNotBeNull("the employee is the only one told the password");

        var password = Regex.Match(message.PlainTextBody, @"Senha:\s*(\S+)").Groups[1].Value;
        password.ShouldNotBeNullOrWhiteSpace();

        var login = await _client.PostAsJsonAsync(
            new Uri("/api/v1/auth/login", UriKind.Relative),
            new { cpf, password, companyId = manager.CompanyId },
            TestContext.Current.CancellationToken);

        // Assert
        var loginBody = await login.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        login.StatusCode.ShouldBe(HttpStatusCode.OK, loginBody);

        // Temporary by design: the app asks for a new one on first use.
        JsonDocument.Parse(loginBody).RootElement
            .GetProperty("pendingActions").EnumerateArray()
            .Select(action => action.GetString())
            .ShouldContain("ChangePassword");
    }

    [Fact]
    public async Task Hire_WhenTheMailServerRefuses_StillRegistersTheEmployee()
    {
        // Arrange — an undelivered password is recovered by resetting it;
        // rolling back the hire leaves somebody unable to clock in tomorrow
        var manager = await SignedInManagerAsync();
        StubEmailSender.Delivers = false;

        // Act
        var response = await PostAsync(manager, NewBody(DocumentFactory.NewCpf()));

        // Assert
        var raw = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.Created, raw);
        JsonDocument.Parse(raw).RootElement
            .GetProperty("welcomeEmailDelivered").GetBoolean()
            .ShouldBeFalse("the caller is told, and the hire stands");
    }

    [Fact]
    public async Task Hire_WithoutAPis_IsAcceptedAndFlagged()
    {
        // Arrange — the AFD needs it; a hire does not. Blocking here would stop
        // somebody working over a field that only matters at export time
        var manager = await SignedInManagerAsync();
        var body = NewBody(DocumentFactory.NewCpf());
        body["pis"] = null;

        // Act
        var response = await PostAsync(manager, body);

        // Assert
        var raw = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.Created, raw);
        JsonDocument.Parse(raw).RootElement
            .GetProperty("isExportableToAfd").GetBoolean()
            .ShouldBeFalse();
    }

    [Fact]
    public async Task Hire_WithACpfAlreadyInTheCompany_IsRefused()
    {
        // Arrange
        var manager = await SignedInManagerAsync();
        var cpf = DocumentFactory.NewCpf();
        await PostAsync(manager, NewBody(cpf));

        // Act
        var response = await PostAsync(manager, NewBody(cpf));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .ShouldContain("ALREADY_REGISTERED");
    }

    [Fact]
    public async Task Hire_WithSeveralBadFields_ReportsAllOfThem()
    {
        // Arrange — a form that rejects one field per round trip is abandoned
        var manager = await SignedInManagerAsync();
        var body = NewBody(DocumentFactory.NewCpf());
        body["cpf"] = "123";
        body["pis"] = "999";
        body["fullName"] = " ";

        // Act
        var response = await PostAsync(manager, body);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);

        var raw = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        raw.ShouldContain("CPF_INVALID");
        raw.ShouldContain("PIS_INVALID");
        raw.ShouldContain("EMPLOYEE_NAME_REQUIRED");
    }

    [Fact]
    public async Task Hire_ByAnEmployeeRatherThanAManager_IsRefused()
    {
        // Arrange — hiring is a manager's act
        var employee = await SignedInEmployeeAsync();

        // Act
        var response = await PostAsync(employee, NewBody(DocumentFactory.NewCpf()));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private async Task<HttpResponseMessage> PostAsync(SignedIn caller, Dictionary<string, object?> body)
    {
        // Awaited inside the using: returning the task would dispose the
        // request, and its content with it, while the send is still reading.
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("/api/v1/employees", UriKind.Relative))
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", caller.AccessToken);

        return await _client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private static Dictionary<string, object?> NewBody(string cpf) =>
        new(StringComparer.Ordinal)
        {
            ["cpf"] = cpf,
            ["fullName"] = "Carlos Novo Funcionário",
            ["email"] = $"novo-{Guid.CreateVersion7():N}@nahora.dev",
            ["pis"] = DocumentFactory.NewPis(),
            ["registration"] = DocumentFactory.NewShortSuffix(),
            ["position"] = "Auxiliar",
            ["department"] = "Operações",
            ["contractType"] = "Clt",
            ["birthDate"] = "1999-07-21",
            ["phone"] = "(11) 96666-5555",
            ["address"] = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["postalCode"] = "01310-100",
                ["street"] = "Avenida Paulista",
                ["number"] = "1500",
                ["district"] = "Bela Vista",
                ["city"] = "São Paulo",
                ["state"] = "SP"
            }
        };

    private Task<SignedIn> SignedInManagerAsync() => SignInAsync(UserRole.Manager);

    private Task<SignedIn> SignedInEmployeeAsync() => SignInAsync(UserRole.Employee);

    private async Task<SignedIn> SignInAsync(UserRole role)
    {
        var account = await fixture.WithScopeAsync(async services =>
        {
            var dbContext = services.GetRequiredService<NaHoraDbContext>();
            var hasher = services.GetRequiredService<IPasswordHasher>();

            return await AuthenticationSeed.CreateAsync(dbContext, hasher, DateTimeOffset.UtcNow, role);
        });

        var login = await _client.PostAsJsonAsync(
            new Uri("/api/v1/auth/login", UriKind.Relative),
            new { cpf = account.Cpf, password = AuthenticationSeed.Password, companyId = account.CompanyId },
            TestContext.Current.CancellationToken);

        var raw = await login.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        login.StatusCode.ShouldBe(HttpStatusCode.OK, raw);

        var token = JsonDocument.Parse(raw).RootElement.GetProperty("accessToken").GetString()!;

        return new SignedIn(account.CompanyId, token);
    }

    private sealed record SignedIn(Guid CompanyId, string AccessToken);
}
