using FaceId.Infrastructure.Configuration;
using FaceId.Infrastructure.Persistence;
using FaceId.Shared.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FaceId.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<StorageOptions>()
            .Bind(configuration.GetSection(StorageOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var database = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>()
                       ?? throw new InvalidOperationException(
                           $"Configuration section '{DatabaseOptions.SectionName}' is missing.");

        services.AddDbContext<FaceIdDbContext>(options =>
            options.UseNpgsql(database.ConnectionString, npgsql =>
                npgsql.CommandTimeout(database.CommandTimeoutSeconds)));

        services.AddServicesFrom(typeof(InfrastructureServiceRegistration).Assembly);

        return services;
    }
}
