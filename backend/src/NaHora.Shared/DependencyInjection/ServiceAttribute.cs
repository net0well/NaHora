using Microsoft.Extensions.DependencyInjection;

namespace NaHora.Shared.DependencyInjection;

/// <summary>
/// Declares the lifetime for a service discovered by the assembly scanner.
/// Takes precedence over the marker interfaces and over convention.
/// </summary>
/// <param name="lifetime">Lifetime to register the implementation with.</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ServiceAttribute(ServiceLifetime lifetime) : Attribute
{
    public ServiceLifetime Lifetime { get; } = lifetime;

    /// <summary>
    /// Service type to register the implementation as. When null, the scanner
    /// resolves it from the implemented interfaces.
    /// </summary>
    public Type? ServiceType { get; init; }
}
