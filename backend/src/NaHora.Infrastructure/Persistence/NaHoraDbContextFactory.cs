using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using NaHora.Infrastructure.Identity;

namespace NaHora.Infrastructure.Persistence;

/// <summary>
/// Builds a context for <c>dotnet ef</c>, which runs without the application
/// host.
/// </summary>
/// <remarks>
/// The connection string is only used to pick the provider and shape the SQL;
/// scaffolding a migration never opens it. Supplying a placeholder keeps design
/// time from depending on a running database or on developer configuration.
/// </remarks>
internal sealed class NaHoraDbContextFactory : IDesignTimeDbContextFactory<NaHoraDbContext>
{
    private const string DesignTimeConnectionString =
        "Host=localhost;Port=5432;Database=nahora_design_time;Username=nahora;Password=design_time";

    public NaHoraDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<NaHoraDbContext>()
            .UseNpgsql(DesignTimeConnectionString)
            .Options;

        // No session at design time, which leaves the tenant filter disabled —
        // correct here, since a migration is not scoped to a company.
        return new NaHoraDbContext(options, new SystemCurrentUser());
    }
}
