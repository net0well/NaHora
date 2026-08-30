using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NaHora.Domain.Users;
using NaHora.Infrastructure.Persistence;
using NaHora.IntegrationTests.Fixtures;

namespace NaHora.IntegrationTests.Security;

/// <summary>
/// That one company cannot reach another company's data.
/// </summary>
/// <remarks>
/// The property the whole product rests on. Isolation is applied twice — a
/// global query filter and an explicit check in each handler — and these tests
/// exercise both through the real HTTP pipeline, because a filter that works
/// in a unit test and is bypassed by a handler is still a leak.
/// </remarks>
public sealed class TenantIsolationTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private readonly HttpClient _client = fixture.CreateClient();

    [Fact]
    public async Task Company_ReadsItsOwnRecord_AndNeverAnother()
    {
        // Arrange — two tenants, provisioned independently
        var first = await SignedInEmployeeAsync();
        var second = await SignedInEmployeeAsync();

        // Act
        var firstBody = await ReadJsonAsync(await GetAsync("/api/v1/companies/me", first.AccessToken));
        var secondBody = await ReadJsonAsync(await GetAsync("/api/v1/companies/me", second.AccessToken));

        // Assert
        firstBody.GetProperty("id").GetGuid().ShouldBe(first.Account.CompanyId);
        secondBody.GetProperty("id").GetGuid().ShouldBe(second.Account.CompanyId);
        firstBody.GetProperty("id").GetGuid().ShouldNotBe(secondBody.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Punches_OfOneCompany_AreInvisibleToAnother()
    {
        // Arrange
        var mine = await SignedInEmployeeAsync();
        var theirs = await SignedInEmployeeAsync();

        await PunchAsync(mine.AccessToken);
        await PunchAsync(theirs.AccessToken);
        await PunchAsync(theirs.AccessToken);

        // Act
        var body = await ReadJsonAsync(await GetAsync("/api/v1/punches/me/history", mine.AccessToken));

        // Assert
        body.GetProperty("totalCount").GetInt32().ShouldBe(1);
    }

    [Fact]
    public async Task Nsr_IsSequentialPerCompany_AndNotAcrossThem()
    {
        // Arrange — the AFD numbers a company's records, not the system's. Two
        // tenants sharing one counter would leave gaps in both.
        var first = await SignedInEmployeeAsync();
        var second = await SignedInEmployeeAsync();

        // Act — interleaved on purpose, so a shared counter would show up
        var firstOne = await PunchAsync(first.AccessToken);
        var secondOne = await PunchAsync(second.AccessToken);
        var firstTwo = await PunchAsync(first.AccessToken);
        var secondTwo = await PunchAsync(second.AccessToken);

        // Assert
        firstTwo.ShouldBe(firstOne + 1);
        secondTwo.ShouldBe(secondOne + 1);
    }

    [Fact]
    public async Task GlobalQueryFilter_HidesAnotherCompanysRows_EvenFromADirectQuery()
    {
        // Arrange — the layer that catches the query somebody forgot to filter
        var mine = await SignedInEmployeeAsync();
        var theirs = await SignedInEmployeeAsync();

        await PunchAsync(theirs.AccessToken);

        // Act — a query with no company predicate at all, run as the first
        // tenant's user. The context is built by hand because the real
        // ICurrentUser reads an HTTP context that a background scope does not
        // have, which would leave the filter disabled and prove nothing.
        var visible = await fixture.WithScopeAsync(async services =>
        {
            var options = services.GetRequiredService<DbContextOptions<NaHoraDbContext>>();

            await using var dbContext = new NaHoraDbContext(
                options,
                new StubCurrentUser(mine.Account.CompanyId, mine.Account.UserId));

            return await dbContext.Punches.CountAsync(TestContext.Current.CancellationToken);
        });

        // Assert
        visible.ShouldBe(0, "the other tenant's punch must not be reachable without a predicate");
    }

    [Fact]
    public async Task Profile_ResolvesTheCallersOwnEmployee()
    {
        // Arrange
        var mine = await SignedInEmployeeAsync();
        var theirs = await SignedInEmployeeAsync();

        // Act
        var body = await ReadJsonAsync(await GetAsync("/api/v1/employees/me", mine.AccessToken));
        var otherBody = await ReadJsonAsync(await GetAsync("/api/v1/employees/me", theirs.AccessToken));

        // Assert
        body.GetProperty("id").GetGuid().ShouldNotBe(otherBody.GetProperty("id").GetGuid());
        body.GetProperty("cpf").GetString().ShouldBe(mine.Account.Cpf);
    }

    [Fact]
    public async Task Login_WithTheRightPasswordButTheWrongCompany_IsRefused()
    {
        // Arrange — the CPF and password are real, the tenant is not theirs
        var mine = await SignedInEmployeeAsync();
        var theirs = await SignedInEmployeeAsync();

        // Act
        var response = await _client.PostAsJsonAsync(
            new Uri("/api/v1/auth/login", UriKind.Relative),
            new
            {
                cpf = mine.Account.Cpf,
                password = AuthenticationSeed.Password,
                companyId = theirs.Account.CompanyId
            },
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private async Task<HttpResponseMessage> GetAsync(string path, string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(path, UriKind.Relative));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return await _client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    /// <summary>Records a punch and returns its NSR.</summary>
    private async Task<long> PunchAsync(string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("/api/v1/punches", UriKind.Relative))
        {
            Content = JsonContent.Create(new
            {
                clientId = Guid.CreateVersion7().ToString("n"),
                location = new
                {
                    latitude = -23.5505,
                    longitude = -46.6333,
                    accuracyMeters = 12.0,
                    capturedAt = DateTimeOffset.UtcNow
                },
                device = new
                {
                    deviceId = "test-device",
                    locationWasMocked = false,
                    deviceClockAt = DateTimeOffset.UtcNow
                },
                offline = (object?)null
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await ReadJsonAsync(response)).GetProperty("nsr").GetInt64();
    }

    private async Task<(SeededAccount Account, string AccessToken)> SignedInEmployeeAsync()
    {
        var account = await fixture.WithScopeAsync(async services =>
        {
            var dbContext = services.GetRequiredService<NaHoraDbContext>();
            var hasher = services.GetRequiredService<IPasswordHasher>();

            var seeded = await AuthenticationSeed.CreateAsync(dbContext, hasher, DateTimeOffset.UtcNow);
            await PunchSeed.HireAsync(dbContext, seeded, DateTimeOffset.UtcNow);

            return seeded;
        });

        var login = await _client.PostAsJsonAsync(
            new Uri("/api/v1/auth/login", UriKind.Relative),
            new { cpf = account.Cpf, password = AuthenticationSeed.Password, companyId = account.CompanyId },
            TestContext.Current.CancellationToken);

        var body = await login.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        login.StatusCode.ShouldBe(HttpStatusCode.OK, body);

        return (account, JsonDocument.Parse(body).RootElement.GetProperty("accessToken").GetString()!);
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(
                await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .RootElement.Clone();
}
