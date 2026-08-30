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
/// Reading back your own punches, over the real HTTP pipeline and against a
/// real database.
/// </summary>
/// <remarks>
/// The employee's copy of their own record. Portaria 671 gives them the right
/// to it, and these are the two endpoints through which the right is actually
/// exercised.
/// </remarks>
public sealed class PunchListingTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private readonly HttpClient _client = fixture.CreateClient();

    [Fact]
    public async Task Day_WithoutAToken_IsRejected()
    {
        // Arrange & Act
        var response = await _client.GetAsync(
            new Uri("/api/v1/punches/me", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task History_WithoutAToken_IsRejected()
    {
        // Arrange & Act
        var response = await _client.GetAsync(
            new Uri("/api/v1/punches/me/history", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Day_BeforeAnyPunch_IsEmptyAndPointsAtTheEntry()
    {
        // Arrange
        var token = await SignedInEmployeeAsync();

        // Act
        var body = await ReadJsonAsync(await GetAsync("/api/v1/punches/me", token));

        // Assert
        body.GetProperty("punches").GetArrayLength().ShouldBe(0);
        body.GetProperty("nextDirection").GetString().ShouldBe("Entry");
        body.GetProperty("timeZoneId").GetString().ShouldBe("America/Sao_Paulo");
    }

    [Fact]
    public async Task Day_AfterPunching_ListsThemInOrderAndPointsAtTheNext()
    {
        // Arrange
        var token = await SignedInEmployeeAsync();
        await PunchAsync(token);
        await PunchAsync(token);

        // Act
        var body = await ReadJsonAsync(await GetAsync("/api/v1/punches/me", token));

        // Assert
        var punches = body.GetProperty("punches").EnumerateArray().ToArray();
        punches.Length.ShouldBe(2);
        punches[0].GetProperty("direction").GetString().ShouldBe("Entry");
        punches[1].GetProperty("direction").GetString().ShouldBe("BreakStart");

        // Advisory, and the server decides again when the punch arrives — but
        // it must agree with what the server would actually record
        body.GetProperty("nextDirection").GetString().ShouldBe("BreakEnd");
    }

    [Fact]
    public async Task Day_NeverCarriesCoordinatesBack()
    {
        // Arrange — a pair of employee and coordinate says where the person was
        // at that minute, and the device is the least protected place to put it
        var token = await SignedInEmployeeAsync();
        await PunchAsync(token);

        // Act
        var raw = await (await GetAsync("/api/v1/punches/me", token))
            .Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        raw.ShouldNotContain("latitude", Case.Insensitive);
        raw.ShouldNotContain("longitude", Case.Insensitive);

        // Accuracy stays: it is what explains a record marked degraded
        raw.ShouldContain("accuracyMeters", Case.Insensitive);
    }

    [Fact]
    public async Task Day_ForADayWithNothingInIt_IsEmptyRatherThanAnError()
    {
        // Arrange
        var token = await SignedInEmployeeAsync();
        await PunchAsync(token);

        // Act — a day long before this company existed
        var body = await ReadJsonAsync(await GetAsync("/api/v1/punches/me?localDate=2020-01-01", token));

        // Assert
        body.GetProperty("localDate").GetString().ShouldBe("2020-01-01");
        body.GetProperty("punches").GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public async Task Day_ForSomeoneWhoIsNotAnEmployee_IsRejected()
    {
        // Arrange — an administrator has no employment record and therefore no
        // punches of their own to read
        var token = await SignedInWithoutEmploymentAsync();

        // Act
        var response = await GetAsync("/api/v1/punches/me", token);

        // Assert — 403, not 422: the request is well formed and the caller is
        // simply not the kind of person this endpoint is for
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden, body);
        body.ShouldContain("EMPLOYEE_RECORD_REQUIRED");
    }

    [Fact]
    public async Task History_ListsTheNewestFirst()
    {
        // Arrange
        var token = await SignedInEmployeeAsync();
        await PunchAsync(token);
        await PunchAsync(token);
        await PunchAsync(token);

        // Act
        var body = await ReadJsonAsync(await GetAsync("/api/v1/punches/me/history", token));

        // Assert
        var entries = body.GetProperty("entries").EnumerateArray().ToArray();
        entries.Length.ShouldBe(3);

        var numbers = entries.Select(entry => entry.GetProperty("nsr").GetInt64()).ToArray();
        numbers.ShouldBe([.. numbers.OrderByDescending(number => number)]);

        body.GetProperty("totalCount").GetInt32().ShouldBe(3);
        body.GetProperty("hasMore").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public async Task History_WithMorePagesToCome_SaysSo()
    {
        // Arrange — hasMore is answered by the server because a client deriving
        // it gets the boundary wrong, and the symptom is a list that never
        // stops loading
        var token = await SignedInEmployeeAsync();
        await PunchAsync(token);
        await PunchAsync(token);
        await PunchAsync(token);

        // Act
        var first = await ReadJsonAsync(await GetAsync("/api/v1/punches/me/history?page=1&pageSize=2", token));
        var second = await ReadJsonAsync(await GetAsync("/api/v1/punches/me/history?page=2&pageSize=2", token));

        // Assert
        first.GetProperty("entries").GetArrayLength().ShouldBe(2);
        first.GetProperty("hasMore").GetBoolean().ShouldBeTrue();

        second.GetProperty("entries").GetArrayLength().ShouldBe(1);
        second.GetProperty("hasMore").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public async Task History_AskedForAnAbsurdPage_IsClampedRatherThanRefused()
    {
        // Arrange — a client asking for ten thousand rows is a bug in the
        // client, and answering with the ceiling keeps the record reachable
        var token = await SignedInEmployeeAsync();
        await PunchAsync(token);

        // Act
        var body = await ReadJsonAsync(
            await GetAsync("/api/v1/punches/me/history?page=0&pageSize=10000", token));

        // Assert
        body.GetProperty("page").GetInt32().ShouldBe(1);
        body.GetProperty("pageSize").GetInt32().ShouldBe(100);
        body.GetProperty("entries").GetArrayLength().ShouldBe(1);
    }

    [Fact]
    public async Task History_ShowsOnlyTheCallersOwnPunches()
    {
        // Arrange — two employees in two companies, punching independently
        var mine = await SignedInEmployeeAsync();
        var theirs = await SignedInEmployeeAsync();

        await PunchAsync(mine);
        await PunchAsync(theirs);
        await PunchAsync(theirs);

        // Act
        var body = await ReadJsonAsync(await GetAsync("/api/v1/punches/me/history", mine));

        // Assert
        body.GetProperty("totalCount").GetInt32().ShouldBe(1, "another employee's punches are not mine to read");
    }

    private async Task<HttpResponseMessage> GetAsync(string path, string accessToken)
    {
        // Awaited inside the using: returning the task would dispose the
        // request while the send is still reading it.
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(path, UriKind.Relative));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return await _client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private async Task PunchAsync(string accessToken)
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
    }

    private Task<string> SignedInEmployeeAsync() => SignInAsync(hire: true);

    private Task<string> SignedInWithoutEmploymentAsync() => SignInAsync(hire: false);

    private async Task<string> SignInAsync(bool hire)
    {
        var account = await fixture.WithScopeAsync(async services =>
        {
            var dbContext = services.GetRequiredService<NaHoraDbContext>();
            var hasher = services.GetRequiredService<IPasswordHasher>();

            var seeded = await AuthenticationSeed.CreateAsync(dbContext, hasher, DateTimeOffset.UtcNow);

            if (hire)
            {
                await PunchSeed.HireAsync(dbContext, seeded, DateTimeOffset.UtcNow);
            }

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
