namespace NaHora.Features.Authentication.GetCurrentUser;

/// <summary>The caller's own profile and effective permissions.</summary>
public sealed record CurrentUserResponse(
    Guid Id,
    string FullName,
    string Email,
    string Role,
    Guid? EmployeeId,
    IReadOnlyList<string> Permissions);
