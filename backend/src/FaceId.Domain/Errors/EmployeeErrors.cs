using FaceId.Shared.Results;

namespace FaceId.Domain.Errors;

/// <summary>
/// Failures of the employee aggregate.
/// </summary>
public static class EmployeeErrors
{
    public static Error CompanyRequired() => Error.Validation(
        "EMPLOYEE_COMPANY_REQUIRED",
        "An employee must belong to a company.",
        "companyId");

    public static Error UserRequired() => Error.Validation(
        "EMPLOYEE_USER_REQUIRED",
        "An employee must have an account in order to punch in.",
        "userId");

    public static Error NameRequired() => Error.Validation(
        "EMPLOYEE_NAME_REQUIRED",
        "Full name is required.",
        "fullName");

    public static Error RegistrationRequired() => Error.Validation(
        "EMPLOYEE_REGISTRATION_REQUIRED",
        "Registration number is required.",
        "registration");

    public static Error NotFound() => Error.NotFound(
        "EMPLOYEE_NOT_FOUND",
        "Employee was not found.");

    public static Error CpfAlreadyRegistered() => Error.Conflict(
        "CPF_ALREADY_REGISTERED",
        "This CPF is already registered in the company.",
        "cpf");

    public static Error RegistrationAlreadyUsed() => Error.Conflict(
        "REGISTRATION_ALREADY_USED",
        "This registration number is already in use.",
        "registration");

    public static Error TerminationDateBeforeHire() => Error.Validation(
        "TERMINATION_DATE_BEFORE_HIRE",
        "Termination date cannot precede the hire date.",
        "terminatedAt");

    public static Error AlreadyTerminated() => Error.Conflict(
        "EMPLOYEE_ALREADY_TERMINATED",
        "This employee has already been terminated.");

    public static Error NotOnLeave() => Error.Conflict(
        "EMPLOYEE_NOT_ON_LEAVE",
        "This employee is not on leave.");

    public static Error NotActive() => Error.Conflict(
        "EMPLOYEE_NOT_ACTIVE",
        "Only an active employee can be placed on leave.");

    /// <summary>
    /// Reported when generating the AFD, never when hiring: a missing PIS
    /// excludes the worker from the regulatory export but must not block the
    /// hire or the punch.
    /// </summary>
    public static Error PisRequiredForAfd() => Error.Validation(
        "EMPLOYEE_PIS_REQUIRED_FOR_AFD",
        "Employee has no PIS and cannot be included in the AFD export.",
        "pis");
}
