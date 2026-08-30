namespace NaHora.Domain.Abstractions;

/// <summary>
/// Marks an entity whose create, update and delete operations are written to the
/// audit trail. Discovery is automatic: the persistence interceptor resolves the
/// marked types once at startup, so there is no manual list to keep in sync.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class AuditAttribute : Attribute
{
    /// <summary>
    /// Properties excluded from the trail. Use for high-churn values that carry
    /// no evidential weight, such as a last-login timestamp.
    /// </summary>
    public string[] IgnoredProperties { get; init; } = [];
}
