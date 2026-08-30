using Microsoft.EntityFrameworkCore;
using NaHora.Domain.Employees;
using NaHora.Domain.ValueObjects;
using NaHora.Shared.DependencyInjection;

namespace NaHora.Infrastructure.Persistence.Repositories;

/// <remarks>
/// Public so Wolverine's generated handler code can construct it directly; an
/// internal adapter would force service location, which the runtime rejects.
/// </remarks>
public sealed class EmployeeRepository(NaHoraDbContext dbContext) : IEmployeeRepository, IScopedService
{
    public Task<Employee?> GetByIdAsync(Guid companyId, Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Employees
            .FirstOrDefaultAsync(
                employee => employee.Id == id && employee.CompanyId == companyId,
                cancellationToken);

    public Task<Employee?> GetByUserIdAsync(
        Guid companyId,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        dbContext.Employees
            .FirstOrDefaultAsync(
                employee => employee.UserId == userId && employee.CompanyId == companyId,
                cancellationToken);

    public Task<Employee?> GetByCpfAsync(Guid companyId, Cpf cpf, CancellationToken cancellationToken = default) =>
        dbContext.Employees
            .FirstOrDefaultAsync(
                employee => employee.Cpf == cpf && employee.CompanyId == companyId,
                cancellationToken);

    public Task<bool> ExistsByCpfAsync(Guid companyId, Cpf cpf, CancellationToken cancellationToken = default) =>
        dbContext.Employees
            .AnyAsync(employee => employee.Cpf == cpf && employee.CompanyId == companyId, cancellationToken);

    /// <summary>
    /// Matches the partial unique index: only employees still on the payroll
    /// count, so a number freed by a departure can be reused.
    /// </summary>
    public Task<bool> ExistsByRegistrationAsync(
        Guid companyId,
        string registration,
        CancellationToken cancellationToken = default) =>
        dbContext.Employees
            .AnyAsync(
                employee => employee.CompanyId == companyId
                            && employee.Registration == registration
                            && employee.Status != EmployeeStatus.Terminated,
                cancellationToken);

    public void Add(Employee employee) => dbContext.Employees.Add(employee);
}
