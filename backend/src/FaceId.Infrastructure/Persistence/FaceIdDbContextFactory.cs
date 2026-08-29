using FaceId.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FaceId.Infrastructure.Persistence;

/// <summary>
/// Builds a context for <c>dotnet ef</c>, which runs without the application
/// host.
/// </summary>
/// <remarks>
/// The connection string is only used to pick the provider and shape the SQL;
/// scaffolding a migration never opens it. Supplying a placeholder keeps design
/// time from depending on a running database or on developer configuration.
/// </remarks>
internal sealed class FaceIdDbContextFactory : IDesignTimeDbContextFactory<FaceIdDbContext>
{
    private const string DesignTimeConnectionString =
        "Host=localhost;Port=5432;Database=faceid_design_time;Username=faceid;Password=design_time";

    public FaceIdDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<FaceIdDbContext>()
            .UseNpgsql(DesignTimeConnectionString)
            .Options;

        // No session at design time, which leaves the tenant filter disabled —
        // correct here, since a migration is not scoped to a company.
        return new FaceIdDbContext(options, new SystemCurrentUser());
    }
}
