using FaceId.Domain.ValueObjects;

namespace FaceId.Domain.Employees;

/// <summary>
/// Access to the employee aggregate.
/// </summary>
public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(Guid companyId, Guid id, CancellationToken cancellationToken = default);

    Task<Employee?> GetByUserIdAsync(Guid companyId, Guid userId, CancellationToken cancellationToken = default);

    Task<Employee?> GetByCpfAsync(Guid companyId, Cpf cpf, CancellationToken cancellationToken = default);

    Task<bool> ExistsByCpfAsync(Guid companyId, Cpf cpf, CancellationToken cancellationToken = default);

    /// <summary>
    /// A registration number is unique among employees who are still on the
    /// payroll. Reusing the number of someone who left is legitimate.
    /// </summary>
    Task<bool> ExistsByRegistrationAsync(
        Guid companyId,
        string registration,
        CancellationToken cancellationToken = default);

    void Add(Employee employee);
}
