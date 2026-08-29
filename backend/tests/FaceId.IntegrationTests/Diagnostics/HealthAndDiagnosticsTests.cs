using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FaceId.IntegrationTests.Contracts;
using FaceId.IntegrationTests.Fixtures;

namespace FaceId.IntegrationTests.Diagnostics;

public sealed class HealthAndDiagnosticsTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private readonly HttpClient _client = fixture.CreateClient();

    [Fact]
    public async Task LivenessProbe_WhenApplicationIsRunning_ReturnsHealthy()
    {
        // Act
        var response = await _client.GetAsync(new Uri("/health/live", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReadinessProbe_WhenDatabaseIsReachable_ReportsPostgresHealthy()
    {
        // Act
        var response = await _client.GetAsync(new Uri("/health/ready", UriKind.Relative), TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var document = JsonDocument.Parse(body);
        document.RootElement.GetProperty("status").GetString().ShouldBe("Healthy");
        document.RootElement
            .GetProperty("components")
            .GetProperty("postgresql")
            .GetProperty("status")
            .GetString()
            .ShouldBe("Healthy");
    }

    [Fact]
    public async Task SystemInfo_WhenRequested_RoutesThroughTheHandlerAndReturnsPayload()
    {
        // Act — exercises the full slice: endpoint, message bus, handler, response
        var response = await _client.GetAsync(new Uri("/api/v1/system/info", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<SystemInfoContract>(TestContext.Current.CancellationToken);
        payload.ShouldNotBeNull();
        payload.Application.ShouldBe("FaceId");
        payload.ServerTimeUtc.ShouldNotBe(default);
    }

    [Fact]
    public async Task UnknownRoute_WhenRequested_ReturnsNotFoundWithoutStackTrace()
    {
        // Act
        var response = await _client.GetAsync(new Uri("/api/v1/does-not-exist", UriKind.Relative), TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert — an error response must never leak internals
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        body.ShouldNotContain("at FaceId.");
        body.ShouldNotContain("Exception");
    }
}
