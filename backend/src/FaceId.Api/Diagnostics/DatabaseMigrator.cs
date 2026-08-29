using FaceId.Infrastructure.Configuration;
using FaceId.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FaceId.Api.Diagnostics;

/// <summary>
/// Applies pending migrations at startup when configuration asks for it.
/// </summary>
/// <remarks>
/// <c>Database:AutoMigrate</c> existed since the persistence layer was written
/// but was never acted on, so a fresh database started with the schema missing
/// and every request failed on a relation that did not exist.
///
/// Development convenience only. In production a migration runs as its own
/// deployment step: applying schema changes as a side effect of a pod starting
/// means several instances racing on the same DDL, and no chance to review what
/// is about to run against live data.
/// </remarks>
internal static class DatabaseMigrator
{
    public static async Task MigrateAsync(WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var options = app.Services.GetRequiredService<IOptions<DatabaseOptions>>().Value;
        var logger = app.Services.GetRequiredService<ILogger<Program>>();

        if (!options.AutoMigrate)
        {
            return;
        }

        if (!app.Environment.IsDevelopment())
        {
            DatabaseMigratorLog.AutoMigrateIgnored(logger);

            return;
        }

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FaceIdDbContext>();

        var pending = (await dbContext.Database.GetPendingMigrationsAsync()).ToArray();

        if (pending.Length == 0)
        {
            return;
        }

        // The array is passed as-is rather than joined: nothing is computed at
        // the call site, and the structured sink renders the list itself.
        DatabaseMigratorLog.ApplyingMigrations(logger, pending.Length, pending);

        await dbContext.Database.MigrateAsync();
    }
}
