namespace NaHora.Features.Authentication.Login;

/// <summary>
/// The signed-in user, as the client needs them.
/// </summary>
/// <remarks>
/// Carries no CPF and no password hash — only what the UI
/// renders and what it uses to decide which controls to show.
/// </remarks>
public sealed record AuthenticatedUserResponse(
    Guid Id,
    string FullName,
    string Role,
    Guid? EmployeeId,
    IReadOnlyList<string> Permissions);
