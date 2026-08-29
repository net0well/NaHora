using FaceId.Domain.Abstractions;
using FaceId.Shared.DependencyInjection;

namespace FaceId.Infrastructure.Identity;

/// <summary>
/// The caller when there is no authenticated session: migrations, the seeder,
/// background workers.
/// </summary>
/// <remarks>
/// Reports <see cref="IsAuthenticated"/> as false, which disables the tenant
/// query filter. That is intended for system work, and it is exactly why the
/// HTTP pipeline must replace this registration with one that reads the token —
/// leaving it in place on a request path would disable isolation.
/// </remarks>
internal sealed class SystemCurrentUser : ICurrentUser, IScopedService
{
    public Guid UserId => Guid.Empty;

    public Guid CompanyId => Guid.Empty;

    public Guid? EmployeeId => null;

    public string Role => "System";

    public bool IsAuthenticated => false;

    public bool HasPermission(string permission) => false;
}
