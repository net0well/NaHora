using NaHora.Domain.ValueObjects;

namespace NaHora.Domain.Companies;

/// <summary>
/// Access to the company aggregate.
/// </summary>
/// <remarks>
/// Deliberately specific rather than a generic CRUD repository: each method is
/// a question the domain actually asks, which keeps query shapes reviewable and
/// indexable.
///
/// Read models for lists and reports do not go through here — they project
/// straight to a DTO, because loading a full aggregate to render a table is
/// waste the database has to pay for.
/// </remarks>
public interface ICompanyRepository
{
    Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Company?> GetByCnpjAsync(Cnpj cnpj, CancellationToken cancellationToken = default);

    /// <summary>CNPJ is unique across the whole system, not per tenant.</summary>
    Task<bool> ExistsByCnpjAsync(Cnpj cnpj, CancellationToken cancellationToken = default);

    void Add(Company company);
}
