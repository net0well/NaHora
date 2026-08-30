using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NaHora.Infrastructure.Persistence;

namespace NaHora.Api.Diagnostics;

/// <summary>
/// Readiness probe for PostgreSQL. Reports unhealthy rather than throwing, so a
/// database outage surfaces as a 503 with a named component instead of a 500.
/// </summary>
internal sealed class DatabaseHealthCheck(NaHoraDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken).ConfigureAwait(false);

            return canConnect
                ? HealthCheckResult.Healthy("PostgreSQL is reachable.")
                : HealthCheckResult.Unhealthy("PostgreSQL is not reachable.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL check failed.", exception);
        }
    }
}
