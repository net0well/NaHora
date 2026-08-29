using System.Xml.Linq;

namespace FaceId.ArchitectureTests;

/// <summary>
/// Guards the dependency list against packages that changed to a commercial
/// licence. Catching this at build time is much cheaper than catching it in a
/// licence audit after the product ships.
/// </summary>
public sealed class PackageLicenseTests
{
    private static readonly string[] ForbiddenPackages =
    [
        "AutoMapper",
        "AutoMapper.Extensions.Microsoft.DependencyInjection",
        "MediatR",
        "MediatR.Contracts",
        "MassTransit",
        "MassTransit.RabbitMQ",
        "FluentAssertions",
        "SixLabors.ImageSharp"
    ];

    [Fact]
    public void CentralPackageManagement_ContainsNoCommercialPackage()
    {
        // Arrange
        var propsPath = LocateDirectoryPackagesProps();
        var document = XDocument.Load(propsPath);

        var declared = document.Descendants("PackageVersion")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .ToArray();

        // Act
        var violations = declared
            .Where(package => ForbiddenPackages.Contains(package, StringComparer.OrdinalIgnoreCase))
            .ToArray();

        // Assert
        violations.ShouldBeEmpty(
            $"Commercially licensed packages found: {string.Join(", ", violations)}. " +
            "See the substitutions listed in the project conventions.");
    }

    [Fact]
    public void CentralPackageManagement_PinsEveryVersion()
    {
        // Arrange
        var document = XDocument.Load(LocateDirectoryPackagesProps());

        // Act
        var floating = document.Descendants("PackageVersion")
            .Select(element => new
            {
                Package = element.Attribute("Include")?.Value,
                Version = element.Attribute("Version")?.Value
            })
            .Where(entry => string.IsNullOrWhiteSpace(entry.Version) || entry.Version!.Contains('*', StringComparison.Ordinal))
            .Select(entry => entry.Package)
            .ToArray();

        // Assert
        floating.ShouldBeEmpty(
            $"Floating versions break reproducible builds: {string.Join(", ", floating)}.");
    }

    private static string LocateDirectoryPackagesProps()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "Directory.Packages.props");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Directory.Packages.props was not found above the test output directory.");
    }
}
