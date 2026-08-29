namespace FaceId.Domain.Users;

/// <summary>
/// Role within a company. There is no level above the tenant.
/// </summary>
/// <remarks>
/// Roles are fixed sets of permissions. Endpoints authorise on the permission,
/// never on the role, so introducing a custom role later changes no endpoint.
/// </remarks>
public enum UserRole
{
    /// <summary>Own data and own attendance only.</summary>
    Employee = 0,

    /// <summary>Employees, schedules and security event review.</summary>
    Manager = 1,

    /// <summary>Everything within the company, including audit and export.</summary>
    Administrator = 2
}
