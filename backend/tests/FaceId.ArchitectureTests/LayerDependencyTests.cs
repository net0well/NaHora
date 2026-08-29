using System.Reflection;
using FaceId.Domain.Abstractions;
using FaceId.Shared.DependencyInjection;

namespace FaceId.ArchitectureTests;

/// <summary>
/// Executable architecture rules. These are not documentation: a violation
/// fails the build, which is the only way a dependency rule survives contact
/// with a deadline.
/// </summary>
public sealed class LayerDependencyTests
{
    private static readonly Assembly DomainAssembly = typeof(Entity).Assembly;

    private static readonly Assembly SharedAssembly = typeof(IScopedService).Assembly;

    [Theory]
    [InlineData("FaceId.Infrastructure")]
    [InlineData("FaceId.Features")]
    [InlineData("FaceId.Api")]
    public void Domain_DoesNotReference_OuterLayers(string forbiddenAssembly)
    {
        // Arrange
        var referenced = DomainAssembly.GetReferencedAssemblies().Select(name => name.Name).ToArray();

        // Act
        var violates = referenced.Contains(forbiddenAssembly, StringComparer.Ordinal);

        // Assert
        violates.ShouldBeFalse(
            $"FaceId.Domain must not reference {forbiddenAssembly}. The domain declares ports; " +
            "outer layers implement them.");
    }

    [Fact]
    public void Domain_ReferencesNothingBeyondShared()
    {
        // Arrange — Shared holds primitives with no domain knowledge (Result,
        // Error, the DI markers), so depending on it does not invert anything.
        var referenced = DomainAssembly.GetReferencedAssemblies()
            .Select(name => name.Name!)
            .Where(name => name.StartsWith("FaceId", StringComparison.Ordinal))
            .ToArray();

        // Act & Assert
        referenced.ShouldBe(["FaceId.Shared"]);
    }

    [Fact]
    public void Shared_DependsOnNoOtherProject()
    {
        // Arrange & Act
        var referenced = SharedAssembly.GetReferencedAssemblies()
            .Select(name => name.Name!)
            .Where(name => name.StartsWith("FaceId", StringComparison.Ordinal))
            .ToArray();

        // Assert — the moment Shared depends on Domain, the direction inverts
        // and Domain's dependency on Shared becomes a cycle.
        referenced.ShouldBeEmpty("FaceId.Shared holds primitives and must not depend on any project.");
    }
}
