using System.Reflection;
using FaceId.Domain.Abstractions;

namespace FaceId.ArchitectureTests;

/// <summary>
/// Executable architecture rules. These are not documentation: a violation fails
/// the build, which is the only way a dependency rule survives contact with a
/// deadline.
/// </summary>
public sealed class LayerDependencyTests
{
    private static readonly Assembly DomainAssembly = typeof(Entity).Assembly;

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
    public void Domain_DependsOnlyOnBaseClassLibrary()
    {
        // Arrange
        var referenced = DomainAssembly.GetReferencedAssemblies()
            .Select(name => name.Name!)
            .Where(name => name.StartsWith("FaceId", StringComparison.Ordinal))
            .ToArray();

        // Act & Assert
        referenced.ShouldBeEmpty("FaceId.Domain must not reference any other project in the solution.");
    }

    [Fact]
    public void Shared_DoesNotReference_Domain()
    {
        // Arrange
        var sharedAssembly = typeof(Shared.DependencyInjection.IScopedService).Assembly;

        // Act
        var referenced = sharedAssembly.GetReferencedAssemblies()
            .Select(name => name.Name!)
            .Where(name => name.StartsWith("FaceId", StringComparison.Ordinal))
            .ToArray();

        // Assert
        referenced.ShouldBeEmpty("FaceId.Shared holds primitives and must not depend on the domain.");
    }
}
