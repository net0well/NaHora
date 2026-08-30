using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;
using NaHora.Domain.Abstractions;
using NaHora.Domain.Authorization;
using NaHora.Shared.DependencyInjection;

namespace NaHora.Infrastructure.Identity;

/// <summary>
/// The authenticated caller, read from the validated access token.
/// </summary>
/// <remarks>
/// Everything comes from claims the server signed. Nothing is read from the
/// request body, the query string or a header — if the client could supply the
/// company, tenant isolation would be decorative.
///
/// Outside a request, or before authentication, this reports itself
/// unauthenticated, which disables the tenant query filter. That is correct for
/// system work and is why the values are also unusable: a handler that acts on
/// <see cref="CompanyId"/> without an authenticated caller gets
/// <see cref="Guid.Empty"/>, which matches nothing.
///
/// Public so Wolverine's generated handler code can construct it directly; an
/// internal adapter would force service location, which the runtime rejects.
/// </remarks>
public sealed class HttpContextCurrentUser(IHttpContextAccessor accessor) : ICurrentUser, IScopedService
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public Guid UserId => ReadGuid(JwtRegisteredClaimNames.Sub) ?? Guid.Empty;

    public Guid CompanyId => ReadGuid(AuthorizationClaims.CompanyId) ?? Guid.Empty;

    public Guid? EmployeeId => ReadGuid(AuthorizationClaims.EmployeeId);

    public string Role => Principal?.FindFirstValue(AuthorizationClaims.Role) ?? "Anonymous";

    public bool IsAuthenticated =>
        Principal?.Identity?.IsAuthenticated == true && CompanyId != Guid.Empty;

    public bool HasPermission(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission) || Principal is null)
        {
            return false;
        }

        return Principal.HasClaim(AuthorizationClaims.Permission, permission);
    }

    private Guid? ReadGuid(string claimType)
    {
        var value = Principal?.FindFirstValue(claimType);

        return Guid.TryParse(value, out var parsed) ? parsed : null;
    }
}
