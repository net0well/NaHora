using Microsoft.EntityFrameworkCore;
using NaHora.Domain.Users;
using NaHora.Domain.ValueObjects;
using NaHora.Shared.DependencyInjection;

namespace NaHora.Infrastructure.Persistence.Repositories;

/// <remarks>
/// Public because Wolverine generates handler code that constructs this type
/// directly. An internal adapter forces the generated code into service
/// location, which the runtime rejects under ServiceLocationPolicy.NotAllowed.
/// The port stays the contract: nothing outside the composition root
/// references this class, and an architecture test keeps it that way.
/// </remarks>
public sealed class UserRepository(NaHoraDbContext dbContext) : IUserRepository, IScopedService
{
    public Task<User?> GetByIdAsync(Guid companyId, Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Users
            .FirstOrDefaultAsync(user => user.Id == id && user.CompanyId == companyId, cancellationToken);

    /// <summary>
    /// Resolves the login credential.
    /// </summary>
    /// <remarks>
    /// Bypasses the global filter deliberately: sign-in happens before a
    /// session exists, so there is no company on the caller yet. The company is
    /// supplied by the caller and applied explicitly here, which keeps the
    /// boundary in the query rather than in an ambient value that is absent at
    /// this point.
    /// </remarks>
    public Task<User?> GetByCpfAsync(Guid companyId, Cpf cpf, CancellationToken cancellationToken = default) =>
        dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(user => user.CompanyId == companyId && user.Cpf == cpf, cancellationToken);

    public Task<bool> ExistsByCpfAsync(Guid companyId, Cpf cpf, CancellationToken cancellationToken = default) =>
        dbContext.Users
            .IgnoreQueryFilters()
            .AnyAsync(user => user.CompanyId == companyId && user.Cpf == cpf, cancellationToken);

    public Task<bool> ExistsByEmailAsync(
        Guid companyId,
        EmailAddress email,
        CancellationToken cancellationToken = default) =>
        dbContext.Users
            .IgnoreQueryFilters()
            .AnyAsync(user => user.CompanyId == companyId && user.Email == email, cancellationToken);

    public void Add(User user) => dbContext.Users.Add(user);
}
