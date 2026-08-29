namespace FaceId.Domain.Authorization;

/// <summary>
/// Names of the custom claims carried by the access token.
/// </summary>
/// <remarks>
/// Constants rather than literals: the token is written in one place and read
/// in another, and a typo between them would fail open — the claim would simply
/// be absent, and an absent company claim is what tenant isolation depends on.
/// </remarks>
public static class AuthorizationClaims
{
    public const string CompanyId = "company_id";
    public const string EmployeeId = "employee_id";
    public const string Role = "role";
    public const string Permission = "perm";
}
