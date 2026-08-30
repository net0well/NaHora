using NaHora.Shared.Results;

namespace NaHora.Domain.Errors;

/// <summary>
/// Failures of the company aggregate.
/// </summary>
public static class CompanyErrors
{
    public static Error LegalNameRequired() => Error.Validation(
        "COMPANY_LEGAL_NAME_REQUIRED",
        "Legal name is required.",
        "legalName");

    public static Error TimeZoneInvalid(string timeZoneId) => Error.Validation(
        "COMPANY_TIMEZONE_INVALID",
        $"Unknown time zone: '{timeZoneId}'.",
        "timeZoneId");

    public static Error NotFound() => Error.NotFound(
        "COMPANY_NOT_FOUND",
        "Company was not found.");

    public static Error CnpjAlreadyRegistered() => Error.Conflict(
        "CNPJ_ALREADY_REGISTERED",
        "This CNPJ is already registered.",
        "cnpj");

    public static Error CnpjNotFoundInRegistry() => Error.Validation(
        "CNPJ_NOT_FOUND_IN_REGISTRY",
        "This CNPJ does not exist in the tax registry.",
        "cnpj");

    public static Error Suspended() => Error.Forbidden(
        "COMPANY_SUSPENDED",
        "The company's access is suspended.");

    public static Error AlreadyConfirmed() => Error.Conflict(
        "COMPANY_ALREADY_CONFIRMED",
        "This company has already been confirmed.");

    public static Error CannotSuspend(object currentStatus) => Error.Conflict(
        "COMPANY_CANNOT_SUSPEND",
        $"A company in state '{currentStatus}' cannot be suspended.");

    public static Error CannotReactivate(object currentStatus) => Error.Conflict(
        "COMPANY_CANNOT_REACTIVATE",
        $"A company in state '{currentStatus}' cannot be reactivated.");

    public static Error AlreadyInactive() => Error.Conflict(
        "COMPANY_ALREADY_INACTIVE",
        "This company is already inactive.");
}
