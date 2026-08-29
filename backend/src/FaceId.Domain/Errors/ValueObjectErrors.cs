using FaceId.Shared.Results;

namespace FaceId.Domain.Errors;

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
}
