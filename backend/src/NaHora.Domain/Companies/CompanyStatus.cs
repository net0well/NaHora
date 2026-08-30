namespace NaHora.Domain.Companies;

/// <summary>
/// Lifecycle of a tenant.
/// </summary>
/// <remarks>
/// <see cref="Inactive"/> is terminal. Historical records survive it, because
/// attendance data carries a statutory retention period that outlives the
/// commercial relationship.
/// </remarks>
public enum CompanyStatus
{
    /// <summary>Registered, awaiting confirmation of the founder's email.</summary>
    PendingEmailConfirmation = 0,

    /// <summary>Operational: users may authenticate and record attendance.</summary>
    Active = 1,

    /// <summary>Temporarily blocked by an administrative action. Reversible.</summary>
    Suspended = 2,

    /// <summary>Closed. Terminal.</summary>
    Inactive = 3
}
