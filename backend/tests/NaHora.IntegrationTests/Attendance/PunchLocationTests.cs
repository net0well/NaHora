using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using NaHora.Domain.Users;
using NaHora.Infrastructure.Persistence;
using NaHora.IntegrationTests.Fixtures;

namespace NaHora.IntegrationTests.Attendance;

/// <summary>
/// Asking where one mark was made.
/// </summary>
/// <remarks>
/// The only route that answers with a coordinate, so it is the only place where
/// getting the boundary wrong leaks somebody's whereabouts. Every test here is
/// about who may ask.
/// </remarks>
public sealed class PunchLocationTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private readonly HttpClient _client = fixture.CreateClient();

    [Fact]
    public async Task Location_WithoutAToken_IsRejected()
    {
        // Arrange & Act
        var response = await _client.GetAsync(
            new Uri($"/api/v1/punches/{Guid.CreateVersion7()}/location", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Location_OfYourOwnPunch_AnswersWithThePositionAndItsRadius()
    {
        // Arrange
        var token = await SignedInEmployeeAsync();
        var punchId = await PunchAsync(token);

        // Act
        var body = await ReadJsonAsync(
            await GetAsync($"/api/v1/punches/{punchId}/location", token));

        // Assert
        body.GetProperty("latitude").GetDouble().ShouldBe(-23.5505, 0.0001);
        body.GetProperty("longitude").GetDouble().ShouldBe(-46.6333, 0.0001);

        // The radius travels with the pin. Without it a map draws certainty the
        // GPS never had.
        body.GetProperty("accuracyMeters").GetDouble().ShouldBe(12.0, 0.0001);
        body.GetProperty("capturedAt").GetDateTimeOffset().ShouldNotBe(default);
    }

    [Fact]
    public async Task Location_OfAPunchThatDoesNotExist_IsNotFound()
    {
        // Arrange
        var token = await SignedInEmployeeAsync();

        // Act
        var response = await GetAsync(
            $"/api/v1/punches/{Guid.CreateVersion7()}/location", token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Location_OfSomebodyElsesPunch_IsNotFoundRatherThanForbidden()
    {
        // Arrange — two separate companies, each with its own employee. Saying
        // 403 would confirm to an outsider that the identifier exists.
        var mine = await SignedInEmployeeAsync();
        var theirs = await SignedInEmployeeAsync();

        var theirPunchId = await PunchAsync(theirs);

        // Act
        var response = await GetAsync($"/api/v1/punches/{theirPunchId}/location", mine);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Location_IsTheOnlyRouteThatCarriesCoordinates()
    {
        // Arrange — the guarantee this endpoint exists to preserve: one mark on
        // request, never a map of everywhere the person has been
        var token = await SignedInEmployeeAsync();
        await PunchAsync(token);

        // Act
        var history = await (await GetAsync("/api/v1/punches/me/history", token))
            .Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        history.ShouldNotContain("latitude", Case.Insensitive);
        history.ShouldNotContain("longitude", Case.Insensitive);
    }

    private async Task<HttpResponseMessage> GetAsync(string path, string accessToken)
    {
        // Awaited inside the using: returning the task would dispose the
        // request while the send is still reading it.
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(path, UriKind.Relative));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return await _client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private async Task<string> PunchAsync(string accessToken)
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

        return (await ReadJsonAsync(response)).GetProperty("id").GetString()!;
    }

    private async Task<string> SignedInEmployeeAsync()
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

        return (await ReadJsonAsync(login)).GetProperty("accessToken").GetString()!;
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(
                await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .RootElement.Clone();
}
