using Microsoft.EntityFrameworkCore;
using NaHora.Domain.Companies;
using NaHora.Domain.ValueObjects;
using NaHora.Shared.DependencyInjection;

namespace NaHora.Infrastructure.Persistence.Repositories;

/// <remarks>
/// Public so Wolverine's generated handler code can construct it directly; an
/// internal adapter would force service location, which the runtime rejects.
/// </remarks>
public sealed class CompanyRepository(NaHoraDbContext dbContext) : ICompanyRepository, IScopedService
{
    /// <summary>
    /// Company is the tenant itself, not a tenant-owned entity, so no global
    /// filter applies. Callers may only reach their own id, which comes from
    /// the token.
    /// </summary>
    public Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Companies.FirstOrDefaultAsync(company => company.Id == id, cancellationToken);

    /// <summary>
    /// Used by sign-up, before any session exists.
    /// </summary>
    public Task<Company?> GetByCnpjAsync(Cnpj cnpj, CancellationToken cancellationToken = default) =>
        dbContext.Companies.FirstOrDefaultAsync(company => company.Cnpj == cnpj, cancellationToken);

    public Task<bool> ExistsByCnpjAsync(Cnpj cnpj, CancellationToken cancellationToken = default) =>
        dbContext.Companies.AnyAsync(company => company.Cnpj == cnpj, cancellationToken);

    public void Add(Company company) => dbContext.Companies.Add(company);
}
