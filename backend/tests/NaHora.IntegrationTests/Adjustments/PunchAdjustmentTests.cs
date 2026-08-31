using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using NaHora.Domain.Adjustments;
using NaHora.Domain.Users;
using NaHora.Infrastructure.Persistence;
using NaHora.IntegrationTests.Fixtures;

namespace NaHora.IntegrationTests.Adjustments;

/// <summary>
/// Asking for a correction, and deciding one, over the real pipeline.
/// </summary>
public sealed class PunchAdjustmentTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private readonly HttpClient _client = fixture.CreateClient();

    private static Guid ForgotJustification =>
        AdjustmentJustificationCatalogue.All
            .Single(j => j.Code == AdjustmentJustificationCatalogue.ForgotToPunch).Id;

    private static Guid OtherJustification =>
        AdjustmentJustificationCatalogue.All
            .Single(j => j.Code == AdjustmentJustificationCatalogue.Other).Id;

    [Fact]
    public async Task Justifications_AreServedAsAFixedCatalogue()
    {
        // Arrange
        var employee = await SignInAsync(UserRole.Employee);

        // Act
        var response = await GetAsync("/api/v1/justifications", employee);

        // Assert
        var body = await ReadJsonAsync(response);
        body.GetArrayLength().ShouldBe(10);

        var other = body.EnumerateArray().Single(entry => entry.GetProperty("code").GetString() == "OTHER");
        other.GetProperty("requiresNotes").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    public async Task Employee_AsksAboutTheirOwnRecord_AndItWaitsForAManager()
    {
        // Arrange — the split the whole flow exists for
        var employee = await SignInAsync(UserRole.Employee);

        // Act
        var response = await PostAsync("/api/v1/adjustments", employee, NewInclusion());

        // Assert
        var raw = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.Created, raw);

        var body = JsonDocument.Parse(raw).RootElement;
        body.GetProperty("status").GetString().ShouldBe("Pending");
        body.GetProperty("counts").GetBoolean()
            .ShouldBeFalse("nothing counts until somebody else agrees");
    }

    [Fact]
    public async Task Manager_AsksAboutSomebodyElsesRecord_AndItCountsImmediately()
    {
        // Arrange — a manager already holds the authority a second step would
        // be checking
        var manager = await SignInAsync(UserRole.Manager);
        var employeeId = await HireAsync(manager);

        var body = NewInclusion();
        body["employeeId"] = employeeId;

        // Act
        var response = await PostAsync("/api/v1/adjustments", manager, body);

        // Assert
        var raw = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.Created, raw);

        var created = JsonDocument.Parse(raw).RootElement;
        created.GetProperty("status").GetString().ShouldBe("Approved");
        created.GetProperty("counts").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    public async Task Manager_AsksAboutTheirOwnRecord_AndItWaitsForSomebodyElse()
    {
        // Arrange — approving a correction to your own timesheet is editing
        // your own timesheet with extra steps
        var manager = await SignInAsync(UserRole.Manager);

        // Act — no employeeId, so it is their own
        var response = await PostAsync("/api/v1/adjustments", manager, NewInclusion());

        // Assert
        var body = await ReadJsonAsync(response);
        body.GetProperty("status").GetString().ShouldBe("Pending");
        body.GetProperty("counts").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public async Task Employee_CannotAskAboutSomebodyElsesRecord()
    {
        // Arrange
        var manager = await SignInAsync(UserRole.Manager);
        var otherEmployeeId = await HireAsync(manager);
        var employee = await SignInAsync(UserRole.Employee);

        var body = NewInclusion();
        body["employeeId"] = otherEmployeeId;

        // Act
        var response = await PostAsync("/api/v1/adjustments", employee, body);

        // Assert — the employeeId is ignored, so it becomes a request about
        // their own record rather than a leak that another exists
        var created = await ReadJsonAsync(response);
        created.GetProperty("employeeId").GetGuid().ShouldNotBe(otherEmployeeId);
    }

    [Fact]
    public async Task Manager_ApprovesAPendingRequest_AndTheAdjustedTimeCounts()
    {
        // Arrange
        var manager = await SignInAsync(UserRole.Manager);
        var employee = await SignInAsync(UserRole.Employee, manager.CompanyId, manager);

        var requested = await ReadJsonAsync(await PostAsync("/api/v1/adjustments", employee, NewInclusion()));
        var adjustmentId = requested.GetProperty("id").GetGuid();

        // Act
        var response = await PostAsync(
            $"/api/v1/adjustments/{adjustmentId}/decision",
            manager,
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["approve"] = true,
                ["notes"] = "Confere com o registro do portão."
            });

        // Assert
        var raw = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, raw);

        var decided = JsonDocument.Parse(raw).RootElement;
        decided.GetProperty("status").GetString().ShouldBe("Approved");
        decided.GetProperty("counts").GetBoolean().ShouldBeTrue();
        decided.GetProperty("decidedAt").ValueKind.ShouldNotBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task Manager_RejectsARequest_AndItIsKept()
    {
        // Arrange — deleting it would hide that somebody asked
        var manager = await SignInAsync(UserRole.Manager);
        var employee = await SignInAsync(UserRole.Employee, manager.CompanyId, manager);

        var requested = await ReadJsonAsync(await PostAsync("/api/v1/adjustments", employee, NewInclusion()));
        var adjustmentId = requested.GetProperty("id").GetGuid();

        // Act
        var response = await PostAsync(
            $"/api/v1/adjustments/{adjustmentId}/decision",
            manager,
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["approve"] = false,
                ["notes"] = "Não bate com a portaria."
            });

        // Assert
        var decided = await ReadJsonAsync(response);
        decided.GetProperty("status").GetString().ShouldBe("Rejected");
        decided.GetProperty("counts").GetBoolean().ShouldBeFalse();

        var mine = await ReadJsonAsync(await GetAsync("/api/v1/adjustments", employee));
        mine.EnumerateArray()
            .Select(entry => entry.GetProperty("id").GetGuid())
            .ShouldContain(adjustmentId, "a refused request stays in the trail");
    }

    [Fact]
    public async Task DecidingTwice_IsRefused()
    {
        // Arrange
        var manager = await SignInAsync(UserRole.Manager);
        var employee = await SignInAsync(UserRole.Employee, manager.CompanyId, manager);

        var requested = await ReadJsonAsync(await PostAsync("/api/v1/adjustments", employee, NewInclusion()));
        var adjustmentId = requested.GetProperty("id").GetGuid();

        var decision = new Dictionary<string, object?>(StringComparer.Ordinal) { ["approve"] = true };
        await PostAsync($"/api/v1/adjustments/{adjustmentId}/decision", manager, decision);

        // Act
        var second = await PostAsync($"/api/v1/adjustments/{adjustmentId}/decision", manager, decision);

        // Assert
        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await second.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .ShouldContain("ADJUSTMENT_ALREADY_DECIDED");
    }

    [Fact]
    public async Task Employee_CannotDecide()
    {
        // Arrange
        var employee = await SignInAsync(UserRole.Employee);
        var requested = await ReadJsonAsync(await PostAsync("/api/v1/adjustments", employee, NewInclusion()));
        var adjustmentId = requested.GetProperty("id").GetGuid();

        // Act
        var response = await PostAsync(
            $"/api/v1/adjustments/{adjustmentId}/decision",
            employee,
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["approve"] = true });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RequestWithTheCatchAllAndNoNotes_IsRefused()
    {
        // Arrange — left free, "Outro" becomes the way to avoid choosing
        var employee = await SignInAsync(UserRole.Employee);
        var body = NewInclusion();
        body["justificationId"] = OtherJustification;
        body["notes"] = "   ";

        // Act
        var response = await PostAsync("/api/v1/adjustments", employee, body);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .ShouldContain("JUSTIFICATION_NOTES_REQUIRED");
    }

    [Fact]
    public async Task RequestForATimeInTheFuture_IsRefused()
    {
        // Arrange — a time nobody has reached is a schedule, not a record
        var employee = await SignInAsync(UserRole.Employee);
        var body = NewInclusion();
        body["occurredAt"] = DateTimeOffset.UtcNow.AddHours(4);

        // Act
        var response = await PostAsync("/api/v1/adjustments", employee, body);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .ShouldContain("ADJUSTMENT_OCCURRED_AT_IN_THE_FUTURE");
    }

    [Fact]
    public async Task Employee_SeesOnlyTheirOwnRequests()
    {
        // Arrange
        var manager = await SignInAsync(UserRole.Manager);
        var mine = await SignInAsync(UserRole.Employee, manager.CompanyId, manager);
        var theirs = await SignInAsync(UserRole.Employee, manager.CompanyId, manager);

        await PostAsync("/api/v1/adjustments", mine, NewInclusion());
        await PostAsync("/api/v1/adjustments", theirs, NewInclusion());
        await PostAsync("/api/v1/adjustments", theirs, NewInclusion());

        // Act
        var body = await ReadJsonAsync(await GetAsync("/api/v1/adjustments", mine));

        // Assert
        body.GetArrayLength().ShouldBe(1);
    }

    [Fact]
    public async Task Manager_SeesTheQueueOfWhatIsStillPending()
    {
        // Arrange
        var manager = await SignInAsync(UserRole.Manager);
        var employee = await SignInAsync(UserRole.Employee, manager.CompanyId, manager);

        await PostAsync("/api/v1/adjustments", employee, NewInclusion());
        await PostAsync("/api/v1/adjustments", employee, NewInclusion());

        // Act
        var body = await ReadJsonAsync(await GetAsync("/api/v1/adjustments?pendingOnly=true", manager));

        // Assert
        body.GetArrayLength().ShouldBe(2);
        body.EnumerateArray()
            .Select(entry => entry.GetProperty("status").GetString())
            .ShouldAllBe(status => status == "Pending");
    }

    private static Dictionary<string, object?> NewInclusion() =>
        new(StringComparer.Ordinal)
        {
            ["occurredAt"] = DateTimeOffset.UtcNow.AddHours(-2),
            ["direction"] = "Exit",
            ["justificationId"] = ForgotJustification,
            ["notes"] = "Esqueci de bater na saída."
        };

    private async Task<Guid> HireAsync(SignedIn manager)
    {
        var response = await PostAsync(
            "/api/v1/employees",
            manager,
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["cpf"] = DocumentFactory.NewCpf(),
                ["fullName"] = "Colega de Trabalho",
                ["email"] = $"colega-{Guid.CreateVersion7():N}@nahora.dev",
                ["registration"] = DocumentFactory.NewShortSuffix()
            });

        return (await ReadJsonAsync(response)).GetProperty("employeeId").GetGuid();
    }

    private async Task<HttpResponseMessage> GetAsync(string path, SignedIn caller)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(path, UriKind.Relative));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", caller.AccessToken);

        return await _client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private async Task<HttpResponseMessage> PostAsync(
        string path,
        SignedIn caller,
        Dictionary<string, object?> body)
    {
        // Awaited inside the using: returning the task would dispose the
        // request while the send is still reading it.
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(path, UriKind.Relative))
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", caller.AccessToken);

        return await _client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Signs somebody in, optionally into a company that already exists so a
    /// manager and an employee share one tenant.
    /// </summary>
    private async Task<SignedIn> SignInAsync(UserRole role, Guid? companyId = null, SignedIn? hiredBy = null)
    {
        if (companyId is not null && hiredBy is not null && role == UserRole.Employee)
        {
            return await HireAndSignInAsync(hiredBy);
        }

        var account = await fixture.WithScopeAsync(async services =>
        {
            var dbContext = services.GetRequiredService<NaHoraDbContext>();
            var hasher = services.GetRequiredService<IPasswordHasher>();

            var seeded = await AuthenticationSeed.CreateAsync(dbContext, hasher, DateTimeOffset.UtcNow, role);
            await PunchSeed.HireAsync(dbContext, seeded, DateTimeOffset.UtcNow);

            return seeded;
        });

        return new SignedIn(account.CompanyId, await TokenAsync(account.Cpf, account.CompanyId));
    }

    /// <summary>
    /// Hires through the API so the employee lands in the manager's company,
    /// then signs in with the password that was emailed.
    /// </summary>
    private async Task<SignedIn> HireAndSignInAsync(SignedIn manager)
    {
        var cpf = DocumentFactory.NewCpf();
        var email = $"colega-{Guid.CreateVersion7():N}@nahora.dev";

        await PostAsync(
            "/api/v1/employees",
            manager,
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["cpf"] = cpf,
                ["fullName"] = "Colega de Trabalho",
                ["email"] = email,
                ["registration"] = DocumentFactory.NewShortSuffix()
            });

        var message = StubEmailSender.LastTo(email);
        var password = System.Text.RegularExpressions.Regex
            .Match(message!.PlainTextBody, @"Senha:\s*(\S+)").Groups[1].Value;

        var login = await _client.PostAsJsonAsync(
            new Uri("/api/v1/auth/login", UriKind.Relative),
            new { cpf, password, companyId = manager.CompanyId },
            TestContext.Current.CancellationToken);

        var token = (await ReadJsonAsync(login)).GetProperty("accessToken").GetString()!;

        return new SignedIn(manager.CompanyId, token);
    }

    private async Task<string> TokenAsync(string cpf, Guid companyId)
    {
        var login = await _client.PostAsJsonAsync(
            new Uri("/api/v1/auth/login", UriKind.Relative),
            new { cpf, password = AuthenticationSeed.Password, companyId },
            TestContext.Current.CancellationToken);

        var raw = await login.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        login.StatusCode.ShouldBe(HttpStatusCode.OK, raw);

        return JsonDocument.Parse(raw).RootElement.GetProperty("accessToken").GetString()!;
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(
                await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .RootElement.Clone();

    private sealed record SignedIn(Guid CompanyId, string AccessToken);
}
