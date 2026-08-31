using Microsoft.EntityFrameworkCore;
using NaHora.Domain.Scheduling;
using NaHora.Shared.DependencyInjection;

namespace NaHora.Infrastructure.Persistence.Repositories;

/// <remarks>
/// Public so Wolverine's generated handler code can construct it directly.
/// </remarks>
public sealed class CompanyWorkScheduleRepository(NaHoraDbContext dbContext)
    : ICompanyWorkScheduleRepository, IScopedService
{
    public Task<CompanyWorkSchedule?> GetByCompanyAsync(
        Guid companyId,
        CancellationToken cancellationToken = default) =>
        dbContext.CompanyWorkSchedules
            .FirstOrDefaultAsync(schedule => schedule.CompanyId == companyId, cancellationToken);

    public void Add(CompanyWorkSchedule schedule) => dbContext.CompanyWorkSchedules.Add(schedule);
}
