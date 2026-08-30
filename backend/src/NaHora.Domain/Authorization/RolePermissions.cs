using NaHora.Domain.Users;

namespace NaHora.Domain.Authorization;

/// <summary>
/// What each role is allowed to do.
/// </summary>
/// <remarks>
/// Roles are fixed sets for now. The indirection is what lets endpoints
/// authorise on a permission, so a future custom role changes this table and
/// nothing else.
/// </remarks>
public static class RolePermissions
{
    private static readonly string[] EmployeePermissions =
    [
        Permissions.ViewOwnAttendance,
        Permissions.RegisterAttendance,
        Permissions.ManageOwnFaceProfile
    ];

    private static readonly string[] ManagerPermissions =
    [
        .. EmployeePermissions,
        Permissions.ViewEmployees,
        Permissions.ManageEmployees,
        Permissions.ManageSchedules,
        Permissions.ViewAllAttendance,
        Permissions.ViewSecurityEvents,
        Permissions.ReviewSecurityEvents,
        Permissions.ViewEvidence,
        Permissions.ExportAttendance
    ];

    private static readonly string[] AdministratorPermissions =
    [
        .. ManagerPermissions,
        Permissions.GenerateAfd,
        Permissions.ViewAudit,
        Permissions.ManageCompany,
        Permissions.ManageRetentionPolicy,
        Permissions.ResetEmployeeFaceProfile
    ];

    public static IReadOnlyList<string> For(UserRole role) => role switch
    {
        UserRole.Employee => EmployeePermissions,
        UserRole.Manager => ManagerPermissions,
        UserRole.Administrator => AdministratorPermissions,
        _ => []
    };
}
