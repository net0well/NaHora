using Microsoft.EntityFrameworkCore;
using NaHora.Domain.Adjustments;
using NaHora.Shared.DependencyInjection;

namespace NaHora.Infrastructure.Persistence.Repositories;

/// <remarks>
/// Public so Wolverine's generated handler code can construct it directly.
/// </remarks>
public sealed class PunchAdjustmentRepository(NaHoraDbContext dbContext)
    : IPunchAdjustmentRepository, IScopedService
{
    public void Add(PunchAdjustment adjustment) => dbContext.PunchAdjustments.Add(adjustment);

    public Task<PunchAdjustment?> GetByIdAsync(
        Guid companyId,
        Guid id,
        CancellationToken cancellationToken = default) =>
        dbContext.PunchAdjustments
            .FirstOrDefaultAsync(
                adjustment => adjustment.CompanyId == companyId && adjustment.Id == id,
                cancellationToken);

    public async Task<IReadOnlyList<PunchAdjustment>> GetPendingAsync(
        Guid companyId,
        CancellationToken cancellationToken = default) =>
        await dbContext.PunchAdjustments
            .Where(adjustment => adjustment.CompanyId == companyId
                                 && adjustment.Status == PunchAdjustmentStatus.Pending)
            .OrderBy(adjustment => adjustment.RequestedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PunchAdjustment>> GetForCompanyAsync(
        Guid companyId,
        CancellationToken cancellationToken = default) =>
        await dbContext.PunchAdjustments
            .Where(adjustment => adjustment.CompanyId == companyId)
            .OrderByDescending(adjustment => adjustment.RequestedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PunchAdjustment>> GetForEmployeeAsync(
        Guid companyId,
        Guid employeeId,
        CancellationToken cancellationToken = default) =>
        await dbContext.PunchAdjustments
            .Where(adjustment => adjustment.CompanyId == companyId && adjustment.EmployeeId == employeeId)
            .OrderByDescending(adjustment => adjustment.RequestedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PunchAdjustment>> GetForPunchesAsync(
        Guid companyId,
        IReadOnlyCollection<Guid> punchIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(punchIds);

        if (punchIds.Count == 0)
        {
            return [];
        }

        return await dbContext.PunchAdjustments
            .Where(adjustment => adjustment.CompanyId == companyId
                                 && adjustment.PunchId != null
                                 && punchIds.Contains(adjustment.PunchId!.Value))
            .OrderBy(adjustment => adjustment.RequestedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PunchAdjustment>> GetInclusionsForDayAsync(
        Guid companyId,
        Guid employeeId,
        DateTimeOffset dayStartUtc,
        DateTimeOffset dayEndUtc,
        CancellationToken cancellationToken = default) =>
        await dbContext.PunchAdjustments
            .Where(adjustment => adjustment.CompanyId == companyId
                                 && adjustment.EmployeeId == employeeId
                                 && adjustment.PunchId == null
                                 && adjustment.Status == PunchAdjustmentStatus.Approved
                                 && adjustment.AdjustedOccurredAt >= dayStartUtc
                                 && adjustment.AdjustedOccurredAt < dayEndUtc)
            .OrderBy(adjustment => adjustment.AdjustedOccurredAt)
            .ToListAsync(cancellationToken);
}
