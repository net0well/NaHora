using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NaHora.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace NaHora.IntegrationTests.Fixtures;

/// <summary>
/// Hosts the API against a real PostgreSQL instance.
/// </summary>
/// <remarks>
/// A real database, not an in-memory provider: the in-memory provider has no
/// timestamptz, no jsonb, no triggers and no FOR UPDATE, so testing against it
/// would validate a system that does not exist. The NSR counter and the
/// append-only triggers this project depends on cannot be exercised any other
/// way.
/// </remarks>
public sealed class ApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("nahora_tests")
        .WithUsername("nahora")
        .WithPassword("nahora_tests")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment(Environments.Development);

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<NaHoraDbContext>>();
            services.RemoveAll<NaHoraDbContext>();

            services.AddDbContext<NaHoraDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString()));
        });
    }

    /// <summary>
    /// Runs work inside a scope with the application's own services, so a test
    /// seeds through the real DbContext and the real password hasher rather
    /// than a parallel construction that could drift from production.
    /// </summary>
    public async Task<TResult> WithScopeAsync<TResult>(Func<IServiceProvider, Task<TResult>> work)
    {
        ArgumentNullException.ThrowIfNull(work);

        using var scope = Services.CreateScope();

        return await work(scope.ServiceProvider);
    }

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();

        // Set before the host exists, and deliberately not through
        // ConfigureAppConfiguration: Program reads the connection string while
        // the host is still being built, so a configuration source added by the
        // factory arrives too late. Environment variables are read by
        // CreateBuilder itself, which is early enough.
        //
        // Overriding the DbContext registration alone was never enough —
        // Wolverine's message store opens its own connection from
        // configuration, and on a build agent that pointed at nothing.
        Environment.SetEnvironmentVariable("Database__ConnectionString", _postgres.GetConnectionString());

        // No broker on an agent. The outbox still runs and keeps its
        // transactional guarantee; only the transport is dropped.
        Environment.SetEnvironmentVariable("Messaging__Enabled", "false");

        // Nothing collects OTLP here, and an exporter retrying into nothing
        // would only slow the suite down.
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", null);

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NaHoraDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        Environment.SetEnvironmentVariable("Database__ConnectionString", null);
        Environment.SetEnvironmentVariable("Messaging__Enabled", null);

        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }
}
