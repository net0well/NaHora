using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace NaHora.Shared.DependencyInjection;

/// <summary>
/// Convention-based service registration. Scans an assembly once at startup and
/// registers eligible implementations, so adding a new service does not require
/// touching a composition root.
/// </summary>
public static class ServiceRegistrationExtensions
{
    private static readonly Type[] LifetimeMarkers =
    [
        typeof(IScopedService),
        typeof(ITransientService),
        typeof(ISingletonService)
    ];

    /// <summary>
    /// Registers every eligible type in <paramref name="assembly"/>.
    /// </summary>
    /// <remarks>
    /// Resolution order, first match wins:
    /// <list type="number">
    /// <item><see cref="ServiceAttribute"/> declared on the type.</item>
    /// <item>A lifetime marker interface.</item>
    /// <item>Convention: <c>IFoo</c> to <c>Foo</c>, when the assembly holds
    /// exactly one concrete implementation of that interface.</item>
    /// </list>
    /// An interface with more than one candidate implementation and no explicit
    /// attribute throws: picking one silently would make the choice invisible.
    /// </remarks>
    public static IServiceCollection AddServicesFrom(this IServiceCollection services, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);

        var candidates = assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false })
            .ToArray();

        foreach (var implementation in candidates)
        {
            var attribute = implementation.GetCustomAttribute<ServiceAttribute>();
            if (attribute is not null)
            {
                RegisterExplicit(services, implementation, attribute);
                continue;
            }

            var markedLifetime = ResolveMarkerLifetime(implementation);
            if (markedLifetime is not null)
            {
                RegisterAgainstInterfaces(services, implementation, markedLifetime.Value);
            }
        }

        RegisterByNamingConvention(services, candidates);

        return services;
    }

    private static void RegisterExplicit(
        IServiceCollection services,
        Type implementation,
        ServiceAttribute attribute)
    {
        if (attribute.ServiceType is not null)
        {
            services.Add(new ServiceDescriptor(attribute.ServiceType, implementation, attribute.Lifetime));
            return;
        }

        var interfaces = RelevantInterfaces(implementation);
        if (interfaces.Length == 0)
        {
            services.Add(new ServiceDescriptor(implementation, implementation, attribute.Lifetime));
            return;
        }

        foreach (var contract in interfaces)
        {
            services.Add(new ServiceDescriptor(contract, implementation, attribute.Lifetime));
        }
    }

    private static ServiceLifetime? ResolveMarkerLifetime(Type implementation)
    {
        if (typeof(IScopedService).IsAssignableFrom(implementation))
        {
            return ServiceLifetime.Scoped;
        }

        if (typeof(ITransientService).IsAssignableFrom(implementation))
        {
            return ServiceLifetime.Transient;
        }

        if (typeof(ISingletonService).IsAssignableFrom(implementation))
        {
            return ServiceLifetime.Singleton;
        }

        return null;
    }

    private static void RegisterAgainstInterfaces(
        IServiceCollection services,
        Type implementation,
        ServiceLifetime lifetime)
    {
        var interfaces = RelevantInterfaces(implementation);
        if (interfaces.Length == 0)
        {
            services.Add(new ServiceDescriptor(implementation, implementation, lifetime));
            return;
        }

        foreach (var contract in interfaces)
        {
            services.Add(new ServiceDescriptor(contract, implementation, lifetime));
        }
    }

    /// <summary>
    /// Registers <c>IFoo</c> to <c>Foo</c> pairs that were not already covered by
    /// an attribute or a marker interface.
    /// </summary>
    private static void RegisterByNamingConvention(IServiceCollection services, Type[] candidates)
    {
        var alreadyRegistered = services
            .Select(descriptor => descriptor.ServiceType)
            .ToHashSet();

        var byContract = new Dictionary<Type, List<Type>>();

        foreach (var implementation in candidates)
        {
            foreach (var contract in RelevantInterfaces(implementation))
            {
                if (contract.Name != $"I{implementation.Name}")
                {
                    continue;
                }

                if (!byContract.TryGetValue(contract, out var implementations))
                {
                    implementations = [];
                    byContract[contract] = implementations;
                }

                implementations.Add(implementation);
            }
        }

        foreach (var (contract, implementations) in byContract)
        {
            if (alreadyRegistered.Contains(contract))
            {
                continue;
            }

            if (implementations.Count > 1)
            {
                var names = string.Join(", ", implementations.Select(type => type.FullName));
                throw new InvalidOperationException(
                    $"Ambiguous registration for '{contract.FullName}': {implementations.Count} candidate " +
                    $"implementations found ({names}). Declare [Service] on the intended implementation.");
            }

            services.Add(new ServiceDescriptor(contract, implementations[0], ServiceLifetime.Scoped));
        }
    }

    /// <summary>
    /// Interfaces the type actually contributes, excluding the lifetime markers
    /// and framework interfaces such as IDisposable.
    /// </summary>
    private static Type[] RelevantInterfaces(Type implementation) =>
        implementation.GetInterfaces()
            .Where(contract => !LifetimeMarkers.Contains(contract))
            .Where(contract => contract.Assembly == implementation.Assembly
                               || contract.Namespace?.StartsWith("NaHora", StringComparison.Ordinal) == true)
            .ToArray();
}
