namespace FaceId.Domain.Companies;

/// <summary>What the registry told us, including the case where it told us nothing.</summary>
public enum RegistryLookupOutcome
{
    /// <summary>The CNPJ exists. Details are populated.</summary>
    Found = 0,

    /// <summary>
    /// The registry answered that the CNPJ does not exist. Conclusive, and
    /// therefore blocking.
    /// </summary>
    NotFound = 1,

    /// <summary>
    /// No answer: timeout, 5xx, or an open circuit. The sign-up proceeds with
    /// local check-digit validation and is flagged as unverified — an outage on
    /// their side must not stop a customer from signing up.
    /// </summary>
    Unavailable = 2
}
