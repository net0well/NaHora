namespace NaHora.Shared.Results;

/// <summary>
/// A single failure.
/// </summary>
/// <param name="Code">
/// Stable identifier, screaming snake case (<c>USER_NAME_REQUIRED</c>). The
/// contract with the client: the Flutter app translates on this, so the message
/// can be reworded without breaking anything.
/// </param>
/// <param name="Message">Human-readable description, for logs and for developers.</param>
/// <param name="Type">Nature of the failure, which decides the HTTP status.</param>
/// <param name="Field">
/// The offending field, when the failure belongs to one. Lets the UI highlight
/// the input rather than showing a banner.
/// </param>
public sealed record Error(
    string Code,
    string Message,
    ErrorType Type = ErrorType.Validation,
    string? Field = null)
{
    public static Error Validation(string code, string message, string? field = null) =>
        new(code, message, ErrorType.Validation, field);

    public static Error NotFound(string code, string message) =>
        new(code, message, ErrorType.NotFound);

    public static Error Conflict(string code, string message, string? field = null) =>
        new(code, message, ErrorType.Conflict, field);

    public static Error Forbidden(string code, string message) =>
        new(code, message, ErrorType.Forbidden);

    public static Error Unauthorized(string code, string message) =>
        new(code, message, ErrorType.Unauthorized);

    public static Error Unavailable(string code, string message) =>
        new(code, message, ErrorType.Unavailable);

    public override string ToString() => $"{Code}: {Message}";
}
