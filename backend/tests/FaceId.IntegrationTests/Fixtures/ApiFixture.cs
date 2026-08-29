using FaceId.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;

namespace FaceId.IntegrationTests.Fixtures;

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
        .WithDatabase("faceid_tests")
        .WithUsername("faceid")
        .WithPassword("faceid_tests")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment(Environments.Development);

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<FaceIdDbContext>>();
            services.RemoveAll<FaceIdDbContext>();

            services.AddDbContext<FaceIdDbContext>(options =>
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

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FaceIdDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }
}
