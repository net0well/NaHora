namespace NaHora.Domain.Scheduling;

/// <summary>
/// Persistence for the company's default hours.
/// </summary>
public interface ICompanyWorkScheduleRepository
{
    /// <summary>The company's schedule, or null when it has never been defined.</summary>
    Task<CompanyWorkSchedule?> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);

    void Add(CompanyWorkSchedule schedule);
}
