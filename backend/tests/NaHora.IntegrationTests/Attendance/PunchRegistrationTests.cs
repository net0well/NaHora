using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NaHora.Domain.Users;
using NaHora.Infrastructure.Persistence;
using NaHora.IntegrationTests.Fixtures;

namespace NaHora.IntegrationTests.Attendance;

/// <summary>
/// Recording a punch, over the real HTTP pipeline and against a real database.
/// </summary>
public sealed class PunchRegistrationTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private readonly HttpClient _client = fixture.CreateClient();

    [Fact]
    public async Task Punch_WithoutAToken_IsRejected()
    {
        // Arrange & Act
        var response = await _client.PostAsJsonAsync(
            new Uri("/api/v1/punches", UriKind.Relative),
            NewPunchBody(),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Punch_WithLocation_IsRecordedWithAnNsrAndTheServerTime()
    {
        // Arrange
        var context = await SignedInEmployeeAsync();

        // Act
        var response = await PostPunchAsync(context.AccessToken, NewPunchBody());

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var body = await ReadJsonAsync(response);
        body.GetProperty("nsr").GetInt64().ShouldBeGreaterThan(0);
        body.GetProperty("direction").GetString().ShouldBe("Entry");
        body.GetProperty("origin").GetString().ShouldBe("Online");
        body.GetProperty("trust").GetString().ShouldBe("Trusted");
        body.GetProperty("alreadyRegistered").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public async Task Punch_WithoutLocation_IsRejected()
    {
        // Arrange — the one thing a punch cannot be without
        var context = await SignedInEmployeeAsync();

        // Act
        var response = await PostPunchAsync(
            context.AccessToken,
            new { clientId = Guid.CreateVersion7().ToString("n"), location = (object?)null });

        // Assert — 422 rather than 400: the request parsed fine, it just asks
        // for something the domain will not accept
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .ShouldContain("LOCATION_REQUIRED");
    }

    [Fact]
    public async Task Punch_WithAWildlyWrongDeviceClock_UsesTheServerTimeAnyway()
    {
        // Arrange — an employee moving the phone clock forward to punch early
        // is the attack the server timestamp exists to defeat
        var context = await SignedInEmployeeAsync();
        var before = DateTimeOffset.UtcNow.AddMinutes(-1);

        // Act
        var response = await PostPunchAsync(
            context.AccessToken,
            NewPunchBody(deviceClockAt: DateTimeOffset.UtcNow.AddHours(3)));

        // Assert
        var body = await ReadJsonAsync(response);
        var occurredAt = body.GetProperty("occurredAt").GetDateTimeOffset();

        occurredAt.ShouldBeGreaterThan(before);
        occurredAt.ShouldBeLessThan(DateTimeOffset.UtcNow.AddMinutes(1));

        // And the lie is kept as evidence rather than discarded
        body.GetProperty("trust").GetString().ShouldBe("NeedsReview");
    }

    [Fact]
    public async Task Punch_WithAMockedLocation_IsRecordedAndFlagged()
    {
        // Arrange — Portaria 671, Art. 78: nothing may restrict the record,
        // and that does not make an exception for suspicion
        var context = await SignedInEmployeeAsync();

        // Act
        var response = await PostPunchAsync(context.AccessToken, NewPunchBody(wasMocked: true));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created, "a suspicious punch is still a punch");
        (await ReadJsonAsync(response)).GetProperty("trust").GetString().ShouldBe("NeedsReview");
    }

    [Fact]
    public async Task Punch_WithUselessAccuracy_IsRecordedAsDegraded()
    {
        // Arrange — inside a warehouse the fix degrades and the employee still
        // has the right to clock in
        var context = await SignedInEmployeeAsync();

        // Act
        var response = await PostPunchAsync(context.AccessToken, NewPunchBody(accuracyMeters: 480));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        (await ReadJsonAsync(response)).GetProperty("trust").GetString().ShouldBe("Degraded");
    }

    [Fact]
    public async Task Punch_SentTwiceWithTheSameClientId_ProducesOnePunch()
    {
        // Arrange — a retry after a timeout must not become a second punch
        var context = await SignedInEmployeeAsync();
        var body = NewPunchBody();

        // Act
        var first = await PostPunchAsync(context.AccessToken, body);
        var second = await PostPunchAsync(context.AccessToken, body);

        // Assert
        first.StatusCode.ShouldBe(HttpStatusCode.Created);
        second.StatusCode.ShouldBe(HttpStatusCode.OK, "nothing was created the second time");

        var firstBody = await ReadJsonAsync(first);
        var secondBody = await ReadJsonAsync(second);

        secondBody.GetProperty("id").GetGuid().ShouldBe(firstBody.GetProperty("id").GetGuid());
        secondBody.GetProperty("nsr").GetInt64().ShouldBe(firstBody.GetProperty("nsr").GetInt64());
        secondBody.GetProperty("alreadyRegistered").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    public async Task Punch_MadeRepeatedly_WalksTheWorkingDayAndNumbersWithoutGaps()
    {
        // Arrange — the direction is the server's decision, not the client's
        var context = await SignedInEmployeeAsync();

        // Act
        var directions = new List<string>();
        var numbers = new List<long>();

        for (var i = 0; i < 4; i++)
        {
            var body = await ReadJsonAsync(await PostPunchAsync(context.AccessToken, NewPunchBody()));
            directions.Add(body.GetProperty("direction").GetString()!);
            numbers.Add(body.GetProperty("nsr").GetInt64());
        }

        // Assert
        directions.ShouldBe(["Entry", "BreakStart", "BreakEnd", "Exit"]);

        // The AFD cannot have gaps, so the numbers must be consecutive
        numbers.ShouldBe([numbers[0], numbers[0] + 1, numbers[0] + 2, numbers[0] + 3]);
    }

    [Fact]
    public async Task Punch_FromTheOfflineQueue_KeepsTheMomentItHappened()
    {
        // Arrange — stamping arrival time would record a punch made at 07:00 as
        // having happened when the phone finally found signal
        var context = await SignedInEmployeeAsync();
        var happenedAt = DateTimeOffset.UtcNow.AddHours(-4);

        // Act
        var response = await PostPunchAsync(context.AccessToken, NewPunchBody(offlineOccurredAt: happenedAt));

        // Assert
        var body = await ReadJsonAsync(response);

        body.GetProperty("origin").GetString().ShouldBe("Offline");
        body.GetProperty("occurredAt").GetDateTimeOffset()
            .ShouldBe(happenedAt, TimeSpan.FromSeconds(1));

        // Offline is ordinary, not suspicious — it lowers what the record
        // proves without implying anything about the person
        body.GetProperty("trust").GetString().ShouldBe("Degraded");
    }

    [Fact]
    public async Task Punch_OnceRecorded_CannotBeChangedEvenFromSql()
    {
        // Arrange — the immutability Portaria 671 requires, enforced by the
        // database rather than by the application's good intentions
        var context = await SignedInEmployeeAsync();
        var punchId = (await ReadJsonAsync(await PostPunchAsync(context.AccessToken, NewPunchBody())))
            .GetProperty("id").GetGuid();

        // Act & Assert
        var update = async () => await fixture.WithScopeAsync(async services =>
        {
            var dbContext = services.GetRequiredService<NaHoraDbContext>();

            return await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE punches SET nsr = nsr + 1000 WHERE id = {punchId}",
                TestContext.Current.CancellationToken);
        });

        await update.ShouldThrowAsync<Npgsql.PostgresException>();

        var delete = async () => await fixture.WithScopeAsync(async services =>
        {
            var dbContext = services.GetRequiredService<NaHoraDbContext>();

            return await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM punches WHERE id = {punchId}",
                TestContext.Current.CancellationToken);
        });

        await delete.ShouldThrowAsync<Npgsql.PostgresException>();
    }

    private static object NewPunchBody(
        double accuracyMeters = 12,
        bool wasMocked = false,
        DateTimeOffset? deviceClockAt = null,
        DateTimeOffset? offlineOccurredAt = null)
    {
        var now = DateTimeOffset.UtcNow;

        return new
        {
            clientId = Guid.CreateVersion7().ToString("n"),
            location = new
            {
                latitude = -23.5505,
                longitude = -46.6333,
                accuracyMeters,
                capturedAt = offlineOccurredAt ?? now
            },
            device = new
            {
                deviceId = "test-device",
                locationWasMocked = wasMocked,
                deviceClockAt = deviceClockAt ?? offlineOccurredAt ?? now
            },
            offline = offlineOccurredAt is null
                ? null
                : new
                {
                    occurredAtEstimate = offlineOccurredAt,
                    monotonicSinceBootMs = 148_230_114L,
                    anchorAt = offlineOccurredAt.Value.AddHours(-1)
                }
        };
    }

    private async Task<HttpResponseMessage> PostPunchAsync(string accessToken, object body)
    {
        // Awaited inside the using: returning the task would dispose the
        // request, and its content with it, while the send is still reading.
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("/api/v1/punches", UriKind.Relative))
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return await _client.SendAsync(request, TestContext.Current.CancellationToken);
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

        var token = (await ReadJsonAsync(login)).GetProperty("accessToken").GetString()!;

        return (account, token);
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        var raw = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        return JsonDocument.Parse(raw).RootElement.Clone();
    }
}
