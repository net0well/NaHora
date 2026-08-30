using System.Reflection;
using FaceId.Domain.Abstractions;

namespace FaceId.ArchitectureTests;

/// <summary>
/// Keeps the pattern naming convention honest.
/// </summary>
/// <remarks>
/// A convention nobody checks is a convention that drifts. These do not try to
/// detect every pattern — only the ones whose absence from a name has actually
/// cost someone a detour through the file.
/// </remarks>
public sealed class NamingConventions
{
    private static Type[] AllTypes =>
    [
        .. typeof(Entity).Assembly.GetTypes(),
        .. typeof(Features.FeaturesServiceRegistration).Assembly.GetTypes(),
        .. typeof(Infrastructure.InfrastructureServiceRegistration).Assembly.GetTypes()
    ];

    [Fact]
    public void EveryRepositoryImplementation_IsNamedRepository()
    {
        // Arrange & Act
        var offenders = AllTypes
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(type => type.GetInterfaces().Any(contract =>
                contract.Name.EndsWith("Repository", StringComparison.Ordinal)))
            .Where(type => !type.Name.EndsWith("Repository", StringComparison.Ordinal))
            .Select(type => type.Name)
            .ToArray();

        // Assert
        offenders.ShouldBeEmpty($"a repository must say so in its name: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void EveryFactory_IsNamedFactory()
    {
        // Arrange & Act — the static factory methods on entities are excluded
        // on purpose: Employee.Hire carries more meaning than EmployeeFactory.
        var offenders = AllTypes
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(type => type.GetInterfaces().Any(contract =>
                contract.Name.Contains("Factory", StringComparison.Ordinal)))
            .Where(type => !type.Name.EndsWith("Factory", StringComparison.Ordinal))
            .Select(type => type.Name)
            .ToArray();

        // Assert
        offenders.ShouldBeEmpty($"a factory must say so in its name: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void NoTypeIsNamedGeneric()
    {
        // Arrange & Act — GenericRepository and friends are banned outright:
        // they trade a reviewable query shape for a signature that hides it.
        var offenders = AllTypes
            .Where(type => type.Name.StartsWith("Generic", StringComparison.Ordinal))
            .Select(type => type.Name)
            .ToArray();

        // Assert
        offenders.ShouldBeEmpty($"generic abstractions are banned: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void EveryHandler_IsNamedHandler()
    {
        // Arrange & Act — Wolverine discovers handlers by name, so a mismatch
        // here is not a style problem: the handler silently never runs.
        var offenders = typeof(Features.FeaturesServiceRegistration).Assembly.GetTypes()
            .Where(type => type is { IsClass: true } && type.Namespace?.Contains("Features", StringComparison.Ordinal) == true)
            .Where(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Any(method => method.Name is "Handle" or "HandleAsync"))
            .Where(type => !type.Name.EndsWith("Handler", StringComparison.Ordinal))
            .Select(type => type.Name)
            .ToArray();

        // Assert
        offenders.ShouldBeEmpty($"a Wolverine handler must end in Handler: {string.Join(", ", offenders)}");
    }
}
