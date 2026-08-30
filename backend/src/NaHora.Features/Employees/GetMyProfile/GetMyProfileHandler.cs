using NaHora.Domain.Abstractions;
using NaHora.Domain.Employees;
using NaHora.Domain.Errors;
using NaHora.Features.Common;
using NaHora.Shared.Results;

namespace NaHora.Features.Employees.GetMyProfile;

/// <summary>
/// The caller's own employment record.
/// </summary>
/// <remarks>
/// Resolved from the token's user, never from an identifier in the request. An
/// employee identifier the caller could set would turn reading a colleague's
/// profile into changing a number.
/// </remarks>
public static class GetMyProfileHandler
{
    public static async Task<Result<GetMyProfileResponse>> Handle(
        GetMyProfileQuery query,
        ICurrentUser currentUser,
        IEmployeeRepository employees,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(currentUser);
        ArgumentNullException.ThrowIfNull(employees);

        var employee = await employees.GetByUserIdAsync(
            currentUser.CompanyId,
            currentUser.UserId,
            cancellationToken);

        if (employee is null)
        {
            return Result.Failure<GetMyProfileResponse>(EmployeeErrors.NotFound());
        }

        return Result.Success(new GetMyProfileResponse(
            employee.Id,
            employee.FullName,
            employee.Cpf.Value,
            employee.Cpf.Formatted,
            employee.Pis?.Value,
            employee.Registration,
            employee.Position,
            employee.Department,
            employee.ContractType.ToString(),
            employee.Status.ToString(),
            employee.HiredAt,
            employee.TerminatedAt,
            employee.BirthDate,
            employee.Email?.Value,
            employee.Phone?.Value,
            employee.Phone?.Formatted,
            employee.Address is null ? null : AddressResponse.From(employee.Address),
            employee.IsExportableToAfd));
    }
}
