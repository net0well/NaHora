using System.Reflection;
using FaceId.Domain.Abstractions;

namespace FaceId.ArchitectureTests;

/// <summary>
/// Rules that keep aggregate boundaries honest.
/// </summary>
public sealed class AggregateRules
{
    private static readonly Assembly DomainAssembly = typeof(Entity).Assembly;

    private static Type[] Entities => [.. DomainAssembly.GetTypes()
        .Where(type => type is { IsClass: true, IsAbstract: false })
        .Where(type => typeof(Entity).IsAssignableFrom(type))];

    private static Type[] AggregateRoots => [.. Entities.Where(type => typeof(AggregateRoot).IsAssignableFrom(type))];

    private static Type[] Repositories => [.. DomainAssembly.GetTypes()
        .Where(type => type.IsInterface && type.Name.EndsWith("Repository", StringComparison.Ordinal))];

    [Fact]
    public void EveryRepository_TargetsAnAggregateRoot()
    {
        // Arrange — a repository for a child entity would let callers load and
        // save it outside its root, which is exactly the boundary the root
        // exists to protect.
        var rootNames = AggregateRoots.Select(type => type.Name).ToHashSet(StringComparer.Ordinal);

        // Act
        var orphans = Repositories
            .Select(repository => repository.Name["I".Length..^"Repository".Length])
            .Where(target => !rootNames.Contains(target))
            .ToArray();

        // Assert
        orphans.ShouldBeEmpty(
            $"only an aggregate root may have a repository. Offending targets: {string.Join(", ", orphans)}");
    }

    [Fact]
    public void EveryAggregateRoot_IsSealed()
    {
        // Arrange & Act — an inheritable root invites a subclass that bypasses
        // the invariants the base enforces.
        var unsealed = AggregateRoots.Where(type => !type.IsSealed).Select(type => type.Name).ToArray();

        // Assert
        unsealed.ShouldBeEmpty($"aggregate roots must be sealed: {string.Join(", ", unsealed)}");
    }

    [Fact]
    public void EveryEntity_ExposesOnlyPrivateSetters()
    {
        // Arrange & Act — state changes belong to named behaviour on the
        // entity. A public setter lets a caller move the entity into a state no
        // rule ever approved.
        var offenders = Entities
            .SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.SetMethod is { IsPublic: true })
                .Select(property => $"{type.Name}.{property.Name}"))
            .ToArray();

        // Assert
        offenders.ShouldBeEmpty($"entity state must change through behaviour: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void EveryTenantOwnedEntity_IsAnEntity()
    {
        // Arrange & Act — the tenant filter is applied by reflection over
        // ITenantOwned against the EF model, so a type carrying the interface
        // without being an entity would silently never be filtered.
        var offenders = DomainAssembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(type => typeof(ITenantOwned).IsAssignableFrom(type))
            .Where(type => !typeof(Entity).IsAssignableFrom(type))
            .Select(type => type.Name)
            .ToArray();

        // Assert
        offenders.ShouldBeEmpty($"ITenantOwned belongs on entities: {string.Join(", ", offenders)}");
    }
}
