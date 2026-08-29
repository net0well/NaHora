namespace FaceId.Domain.Abstractions;

/// <summary>
/// The authenticated caller, resolved from the access token.
/// </summary>
/// <remarks>
/// <see cref="CompanyId"/> comes from the token and never from the request body,
/// query string or a header. If the client could supply it, tenant isolation
/// would be decorative.
/// </remarks>
public interface ICurrentUser
{
    Guid UserId { get; }

    Guid CompanyId { get; }

    Guid? EmployeeId { get; }

    string Role { get; }

    bool IsAuthenticated { get; }

    bool HasPermission(string permission);
}
