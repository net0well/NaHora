using Microsoft.EntityFrameworkCore;
using NaHora.Domain.Scheduling;
using NaHora.Shared.DependencyInjection;

namespace NaHora.Infrastructure.Persistence.Repositories;

/// <remarks>
/// Public so Wolverine's generated handler code can construct it directly.
/// </remarks>
public sealed class EmployeeWorkScheduleRepository(NaHoraDbContext dbContext)
    : IEmployeeWorkScheduleRepository, IScopedService
{
    public async Task<IReadOnlyList<EmployeeWorkSchedule>> GetForEmployeeAsync(
        Guid companyId,
        Guid employeeId,
        CancellationToken cancellationToken = default) =>
        await dbContext.EmployeeWorkSchedules
            .Where(schedule => schedule.CompanyId == companyId && schedule.EmployeeId == employeeId)
            .OrderBy(schedule => schedule.EffectiveFrom)
            .ToListAsync(cancellationToken);

    public Task<EmployeeWorkSchedule?> GetCurrentAsync(
        Guid companyId,
        Guid employeeId,
        CancellationToken cancellationToken = default) =>
        dbContext.EmployeeWorkSchedules
            .Where(schedule => schedule.CompanyId == companyId
                               && schedule.EmployeeId == employeeId
                               && schedule.EffectiveTo == null)
            .OrderByDescending(schedule => schedule.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);

    public void Add(EmployeeWorkSchedule schedule) => dbContext.EmployeeWorkSchedules.Add(schedule);
}
