namespace NaHora.Domain.Scheduling;

/// <summary>
/// Persistence for per-employee hours.
/// </summary>
/// <remarks>
/// Reads return every version rather than only the current one, because
/// answering "what were their hours in March" is the reason the versions exist.
/// </remarks>
public interface IEmployeeWorkScheduleRepository
{
    /// <summary>Every version for the employee, oldest period first.</summary>
    Task<IReadOnlyList<EmployeeWorkSchedule>> GetForEmployeeAsync(
        Guid companyId,
        Guid employeeId,
        CancellationToken cancellationToken = default);

    /// <summary>The version still open, if there is one.</summary>
    Task<EmployeeWorkSchedule?> GetCurrentAsync(
        Guid companyId,
        Guid employeeId,
        CancellationToken cancellationToken = default);

    void Add(EmployeeWorkSchedule schedule);
}
