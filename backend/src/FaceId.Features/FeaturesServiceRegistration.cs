using FaceId.Features.Abstractions;
using FaceId.Shared.DependencyInjection;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace FaceId.Features;

public static class FeaturesServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var assembly = typeof(FeaturesServiceRegistration).Assembly;

        services.AddServicesFrom(assembly);

        services.AddValidatorsFrom(assembly);

        foreach (var endpointGroup in assembly.GetTypes()
                     .Where(type => type is { IsClass: true, IsAbstract: false })
                     .Where(type => typeof(IEndpointGroup).IsAssignableFrom(type)))
        {
            services.AddSingleton(typeof(IEndpointGroup), endpointGroup);
        }

        return services;
    }

    /// <summary>
    /// Wires every discovered slice into the router. Called once from the host.
    /// </summary>
    public static IEndpointRouteBuilder MapFeatureEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        foreach (var group in endpoints.ServiceProvider.GetServices<IEndpointGroup>())
        {
            group.Map(endpoints);
        }

        return endpoints;
    }

    private static void AddValidatorsFrom(this IServiceCollection services, global::System.Reflection.Assembly assembly)
    {
        var validatorInterface = typeof(FluentValidation.IValidator<>);

        foreach (var type in assembly.GetTypes().Where(candidate => candidate is { IsClass: true, IsAbstract: false }))
        {
            foreach (var contract in type.GetInterfaces()
                         .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == validatorInterface))
            {
                services.AddScoped(contract, type);
            }
        }
    }
}
