using NaHora.Domain.ValueObjects;

namespace NaHora.Domain.Users;

/// <summary>
/// Access to the user aggregate.
/// </summary>
/// <remarks>
/// Every method is scoped to a company. The global query filter already
/// constrains reads, but the parameter makes the boundary visible at the call
/// site: an isolation rule that is only enforced invisibly is one nobody
/// notices when it breaks.
/// </remarks>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid companyId, Guid id, CancellationToken cancellationToken = default);

    /// <summary>Resolves the login credential. Unique per company, not globally.</summary>
    Task<User?> GetByCpfAsync(Guid companyId, Cpf cpf, CancellationToken cancellationToken = default);

    Task<bool> ExistsByCpfAsync(Guid companyId, Cpf cpf, CancellationToken cancellationToken = default);

    Task<bool> ExistsByEmailAsync(Guid companyId, EmailAddress email, CancellationToken cancellationToken = default);

    void Add(User user);
}
