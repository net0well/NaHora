using NaHora.Features.Common;

namespace NaHora.Features.Employees.HireEmployee;

/// <summary>
/// Registers an employee and the account they sign in with.
/// </summary>
/// <remarks>
/// No password. It is generated and emailed to the employee — a manager who
/// chooses it knows the credential of the person whose hours they audit.
/// </remarks>
/// <param name="Pis">
/// Optional. Required by the AFD export, so its absence surfaces as a warning
/// on the record and never blocks a hire.
/// </param>
/// <param name="HiredAt">Null means today, in company time.</param>
public sealed record HireEmployeeCommand(
    string? Cpf,
    string? FullName,
    string? Email,
    string? Pis,
    string? Registration,
    string? Position,
    string? Department,
    string? ContractType,
    DateOnly? BirthDate,
    DateOnly? HiredAt,
    string? Phone,
    AddressInput? Address);
