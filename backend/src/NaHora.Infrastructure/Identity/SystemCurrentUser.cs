using NaHora.Domain.Abstractions;

namespace NaHora.Infrastructure.Identity;

/// <summary>
/// The caller when there is no authenticated session: migrations, the seeder,
/// background workers.
/// </summary>
/// <remarks>
/// Reports <see cref="IsAuthenticated"/> as false, which disables the tenant
/// query filter — correct for system work.
///
/// Deliberately not registered in DI. The request pipeline resolves
/// <see cref="HttpContextCurrentUser"/> instead, and two competing
/// registrations for the same port would let the last one win silently. This
/// type is constructed explicitly, by the design-time factory and by tests.
/// </remarks>
internal sealed class SystemCurrentUser : ICurrentUser
{
    public Guid UserId => Guid.Empty;

    public Guid CompanyId => Guid.Empty;

    public Guid? EmployeeId => null;

    public string Role => "System";

    public bool IsAuthenticated => false;

    public bool HasPermission(string permission) => false;
}
