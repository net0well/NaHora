using Microsoft.Extensions.DependencyInjection;
using NaHora.Shared.DependencyInjection;
using NaHora.UnitTests.DependencyInjection.Fakes;

namespace NaHora.UnitTests.DependencyInjection;

public sealed class ServiceRegistrationTests
{
    [Fact]
    public void AddServicesFrom_WhenTypeUsesScopedMarker_RegistersAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddServicesFrom(typeof(ServiceRegistrationTests).Assembly);

        // Assert
        var descriptor = services.Single(item => item.ServiceType == typeof(ISampleScopedService));
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
        descriptor.ImplementationType.ShouldBe(typeof(SampleScopedService));
    }

    [Fact]
    public void AddServicesFrom_WhenTypeUsesSingletonMarker_RegistersAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddServicesFrom(typeof(ServiceRegistrationTests).Assembly);

        // Assert
        var descriptor = services.Single(item => item.ServiceType == typeof(ISampleSingletonService));
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddServicesFrom_WhenAttributeDeclaresLifetime_AttributeWinsOverMarker()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddServicesFrom(typeof(ServiceRegistrationTests).Assembly);

        // Assert
        var descriptor = services.Single(item => item.ServiceType == typeof(ISampleOverriddenService));
        descriptor.Lifetime.ShouldBe(
            ServiceLifetime.Transient,
            "an explicit [Service] attribute must take precedence over the marker interface");
    }

    [Fact]
    public void AddServicesFrom_RegistersEveryServiceItCanResolve()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddServicesFrom(typeof(ServiceRegistrationTests).Assembly);

        // Act
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        using var scope = provider.CreateScope();

        // Assert
        scope.ServiceProvider.GetRequiredService<ISampleScopedService>().ShouldNotBeNull();
        scope.ServiceProvider.GetRequiredService<ISampleSingletonService>().ShouldNotBeNull();
        scope.ServiceProvider.GetRequiredService<ISampleOverriddenService>().ShouldNotBeNull();
    }
}
