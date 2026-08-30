namespace NaHora.Domain.Abstractions;

/// <summary>
/// Every business entity belongs to exactly one company. Implementing this
/// interface is what makes an entity subject to the global tenant filter and to
/// the architecture test that guards against a new entity escaping isolation.
/// </summary>
public interface ITenantOwned
{
    Guid CompanyId { get; }
}
