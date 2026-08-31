namespace NaHora.Features.Employees.HireEmployee;

/// <summary>
/// The registered employee.
/// </summary>
/// <remarks>
/// Carries no password. It went to the employee by email, and returning it here
/// would put it in the manager's browser, their history and any log that
/// records a response body.
/// </remarks>
/// <param name="WelcomeEmailDelivered">
/// False when the mail server refused. The hire still happened — an
/// undelivered password is recovered by resetting it, and rolling back the
/// employment record would leave somebody unable to clock in tomorrow.
/// </param>
public sealed record HireEmployeeResponse(
    Guid EmployeeId,
    Guid UserId,
    string FullName,
    string Cpf,
    string Registration,
    string Email,
    bool WelcomeEmailDelivered,
    bool IsExportableToAfd);
