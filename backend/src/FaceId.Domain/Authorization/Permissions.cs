namespace FaceId.Domain.Authorization;

/// <summary>
/// Every permission the system authorises on.
/// </summary>
/// <remarks>
/// Endpoints require a permission, never a role. When a custom role appears,
/// no endpoint has to change — only the mapping does.
/// </remarks>
public static class Permissions
{
    public const string ViewOwnAttendance = "attendance:view-own";
    public const string RegisterAttendance = "attendance:register";
    public const string ManageOwnFaceProfile = "face-profile:manage-own";

    public const string ViewEmployees = "employees:view";
    public const string ManageEmployees = "employees:manage";
    public const string ManageSchedules = "schedules:manage";
    public const string ViewAllAttendance = "attendance:view-all";

    public const string ViewSecurityEvents = "security-events:view";
    public const string ReviewSecurityEvents = "security-events:review";
    public const string ViewEvidence = "evidence:view";

    public const string ExportAttendance = "attendance:export";
    public const string GenerateAfd = "compliance:generate-afd";
    public const string ViewAudit = "audit:view";

    public const string ManageCompany = "company:manage";
    public const string ManageRetentionPolicy = "retention-policy:manage";
    public const string ResetEmployeeFaceProfile = "face-profile:reset";

    /// <summary>Used to register one authorisation policy per permission.</summary>
    public static IReadOnlyList<string> All { get; } =
    [
        ViewOwnAttendance,
        RegisterAttendance,
        ManageOwnFaceProfile,
        ViewEmployees,
        ManageEmployees,
        ManageSchedules,
        ViewAllAttendance,
        ViewSecurityEvents,
        ReviewSecurityEvents,
        ViewEvidence,
        ExportAttendance,
        GenerateAfd,
        ViewAudit,
        ManageCompany,
        ManageRetentionPolicy,
        ResetEmployeeFaceProfile
    ];
}
