using NaHora.Shared.Results;

namespace NaHora.Domain.Errors;

/// <summary>
/// Failures produced while parsing the value objects.
/// </summary>
public static class ValueObjectErrors
{
    public static Error CpfInvalid(string? field = null) => Error.Validation(
        "CPF_INVALID",
        "CPF is not a valid number.",
        field);

    public static Error CnpjInvalid(string? field = null) => Error.Validation(
        "CNPJ_INVALID",
        "CNPJ is not a valid number.",
        field);

    public static Error PisInvalid(string? field = null) => Error.Validation(
        "PIS_INVALID",
        "PIS/PASEP is not a valid number.",
        field);

    public static Error EmailInvalid(string? field = null) => Error.Validation(
        "EMAIL_INVALID",
        "Email address is not valid.",
        field);

    public static Error PostalCodeInvalid(string? field = null) => Error.Validation(
        "POSTAL_CODE_INVALID",
        "Postal code must have eight digits.",
        field);

    public static Error PhoneNumberInvalid(string? field = null) => Error.Validation(
        "PHONE_NUMBER_INVALID",
        "Phone number must have an area code and eight or nine digits.",
        field);

    public static Error AddressStreetRequired(string? field = null) => Error.Validation(
        "ADDRESS_STREET_REQUIRED",
        "Street is required.",
        field);

    public static Error AddressNumberRequired(string? field = null) => Error.Validation(
        "ADDRESS_NUMBER_REQUIRED",
        "Street number is required. Use \"S/N\" when there is none.",
        field);

    public static Error AddressCityRequired(string? field = null) => Error.Validation(
        "ADDRESS_CITY_REQUIRED",
        "City is required.",
        field);

    public static Error AddressStateInvalid(string? field = null) => Error.Validation(
        "ADDRESS_STATE_INVALID",
        "State must be a two-letter Brazilian state code.",
        field);
}
