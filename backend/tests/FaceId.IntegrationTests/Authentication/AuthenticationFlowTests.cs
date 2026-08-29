using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FaceId.Domain.Users;
using FaceId.Infrastructure.Persistence;
using FaceId.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace FaceId.IntegrationTests.Authentication;

public sealed class AuthenticationFlowTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private readonly HttpClient _client = fixture.CreateClient();

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokensAndProfile()
    {
        // Arrange
        var account = await SeedAsync();

        // Act
        var response = await PostLoginAsync(account.Cpf, AuthenticationSeed.Password, account.CompanyId);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await ReadJsonAsync(response);
        body.GetProperty("accessToken").GetString().ShouldNotBeNullOrWhiteSpace();
        body.GetProperty("refreshToken").GetString().ShouldNotBeNullOrWhiteSpace();
        body.GetProperty("expiresIn").GetInt32().ShouldBeGreaterThan(0);
        body.GetProperty("user").GetProperty("role").GetString().ShouldBe("Administrator");
    }

    [Fact]
    public async Task Login_ResponseNeverCarriesTheCpfOrThePasswordHash()
    {
        // Arrange
        var account = await SeedAsync();

        // Act
        var response = await PostLoginAsync(account.Cpf, AuthenticationSeed.Password, account.CompanyId);
        var raw = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        raw.ShouldNotContain(account.Cpf);
        raw.ShouldNotContain("argon2id");
        raw.ShouldNotContain("passwordHash", Case.Insensitive);
    }

    [Fact]
    public async Task Login_WithWrongPasswordAndWithUnknownCpf_AreIndistinguishable()
    {
        // Arrange
        var account = await SeedAsync();

        // Act
        var wrongPassword = await PostLoginAsync(account.Cpf, "Wrong-Password-1!", account.CompanyId);
        var unknownCpf = await PostLoginAsync("111.444.777-35", AuthenticationSeed.Password, account.CompanyId);

        // Assert — telling them apart would let an attacker enumerate who works
        // at the company.
        wrongPassword.StatusCode.ShouldBe(unknownCpf.StatusCode);

        var first = await ReadJsonAsync(wrongPassword);
        var second = await ReadJsonAsync(unknownCpf);

        first.GetProperty("code").GetString().ShouldBe("INVALID_CREDENTIALS");
        second.GetProperty("code").GetString().ShouldBe("INVALID_CREDENTIALS");
        first.GetProperty("detail").GetString().ShouldBe(second.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task CurrentUser_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync(
            new Uri("/api/v1/auth/me", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CurrentUser_WithAccessToken_ReturnsProfileAndPermissions()
    {
        // Arrange
        var account = await SeedAsync();
        var tokens = await SignInAsync(account);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        // Act
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await ReadJsonAsync(response);
        body.GetProperty("fullName").GetString().ShouldBe("Maria Souza");
        body.GetProperty("permissions").GetArrayLength().ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Refresh_RotatesTheTokenPair()
    {
        // Arrange
        var account = await SeedAsync();
        var original = await SignInAsync(account);

        // Act
        var response = await PostRefreshAsync(original.RefreshToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await ReadJsonAsync(response);
        body.GetProperty("refreshToken").GetString().ShouldNotBe(
            original.RefreshToken,
            "every refresh must issue a new token, otherwise a leaked one stays valid forever");
    }

    [Fact]
    public async Task Refresh_WhenARotatedTokenIsPresentedAgain_RevokesTheWholeFamily()
    {
        // Arrange
        var account = await SeedAsync();
        var original = await SignInAsync(account);

        var rotated = await ReadJsonAsync(await PostRefreshAsync(original.RefreshToken));
        var currentToken = rotated.GetProperty("refreshToken").GetString()!;

        // Act — replaying the token that was already exchanged
        var replay = await PostRefreshAsync(original.RefreshToken);

        // Assert
        replay.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await ReadJsonAsync(replay)).GetProperty("code").GetString().ShouldBe("REFRESH_TOKEN_REUSED");

        // And the token that was still valid is now dead too: the legitimate
        // holder and the thief cannot be told apart, so the chain goes.
        var afterBreach = await PostRefreshAsync(currentToken);
        afterBreach.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithAnUnknownToken_ReturnsUnauthorized()
    {
        // Act
        var response = await PostRefreshAsync("this-token-was-never-issued");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await ReadJsonAsync(response)).GetProperty("code").GetString().ShouldBe("REFRESH_TOKEN_INVALID");
    }

    [Fact]
    public async Task Logout_RevokesTheRefreshToken()
    {
        // Arrange
        var account = await SeedAsync();
        var tokens = await SignInAsync(account);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout")
        {
            Content = JsonContent.Create(new { refreshToken = tokens.RefreshToken })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        // Act
        var logout = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert — logout is only real if the refresh stops working
        logout.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await PostRefreshAsync(tokens.RefreshToken)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private async Task<SeededAccount> SeedAsync() =>
        await fixture.WithScopeAsync(async services =>
        {
            var dbContext = services.GetRequiredService<FaceIdDbContext>();
            var hasher = services.GetRequiredService<IPasswordHasher>();

            return await AuthenticationSeed.CreateAsync(dbContext, hasher, DateTimeOffset.UtcNow);
        });

    private async Task<(string AccessToken, string RefreshToken)> SignInAsync(SeededAccount account)
    {
        var body = await ReadJsonAsync(
            await PostLoginAsync(account.Cpf, AuthenticationSeed.Password, account.CompanyId));

        return (body.GetProperty("accessToken").GetString()!, body.GetProperty("refreshToken").GetString()!);
    }

    private Task<HttpResponseMessage> PostLoginAsync(string cpf, string password, Guid companyId) =>
        _client.PostAsJsonAsync(
            new Uri("/api/v1/auth/login", UriKind.Relative),
            new { cpf, password, companyId },
            TestContext.Current.CancellationToken);

    private Task<HttpResponseMessage> PostRefreshAsync(string refreshToken) =>
        _client.PostAsJsonAsync(
            new Uri("/api/v1/auth/refresh", UriKind.Relative),
            new { refreshToken },
            TestContext.Current.CancellationToken);

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        var raw = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        return JsonDocument.Parse(raw).RootElement.Clone();
    }
}
