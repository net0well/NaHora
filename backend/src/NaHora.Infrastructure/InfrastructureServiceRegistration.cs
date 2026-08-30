using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using NaHora.Domain.Companies;
using NaHora.Domain.RealTime;
using NaHora.Infrastructure.Configuration;
using NaHora.Infrastructure.Persistence;
using NaHora.Infrastructure.RealTime;
using NaHora.Infrastructure.Registry;
using NaHora.Shared.Configuration;
using NaHora.Shared.DependencyInjection;
using Polly;
using Polly.Timeout;
using Wolverine.EntityFrameworkCore;

namespace NaHora.Infrastructure;

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

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<PasswordHashingOptions>()
            .Bind(configuration.GetSection(PasswordHashingOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var database = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>()
                       ?? throw new InvalidOperationException(
                           $"Configuration section '{DatabaseOptions.SectionName}' is missing.");

        // Registered through Wolverine rather than AddDbContext: it is what
        // enrols the context in the transactional outbox, so a message is only
        // sent if the transaction that produced it committed. It also lets the
        // generated handler code inline the context instead of falling back to
        // service location.
        services.AddDbContextWithWolverineIntegration<NaHoraDbContext>(options =>
            options.UseNpgsql(database.ConnectionString, npgsql =>
                npgsql.CommandTimeout(database.CommandTimeoutSeconds)));

        // The current-user adapter reads claims off the ambient request.
        services.AddHttpContextAccessor();

        AddRegistryLookup(services, configuration);

        AddRealTime(services, configuration);

        services.AddServicesFrom(typeof(InfrastructureServiceRegistration).Assembly);

        return services;
    }

    /// <summary>
    /// The registry of connected observers, behind both of its ports.
    /// </summary>
    /// <remarks>
    /// Registered by hand, and it has to be: the two interfaces must resolve to
    /// the same instance. Registering the class once per interface would give
    /// the endpoint a registry nobody broadcasts to, and the relay a registry
    /// nobody observes — with no error anywhere, just silence on the stream.
    ///
    /// Singleton because the registration outlives every request that reads it.
    /// </remarks>
    private static void AddRealTime(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RealTimeOptions>()
            .Bind(configuration.GetSection(RealTimeOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<InMemoryRealTimeObserverRegistry>();
        services.AddSingleton<IRealTimeBroadcaster>(provider =>
            provider.GetRequiredService<InMemoryRealTimeObserverRegistry>());
        services.AddSingleton<IRealTimeObserverRegistry>(provider =>
            provider.GetRequiredService<InMemoryRealTimeObserverRegistry>());
    }

    /// <summary>
    /// The tax registry client and its resilience pipeline.
    /// </summary>
    /// <remarks>
    /// Retry only on transient failure. A 404 means the CNPJ does not exist —
    /// repeating it cannot change the answer and only makes the customer wait.
    ///
    /// The jitter matters more than it looks: without it, every client that
    /// failed together comes back together, and the retry becomes the second
    /// wave of the outage.
    /// </remarks>
    private static void AddRegistryLookup(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RegistryOptions>()
            .Bind(configuration.GetSection(RegistryOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var registry = configuration.GetSection(RegistryOptions.SectionName).Get<RegistryOptions>()
                       ?? new RegistryOptions();

        // A named client, not a typed one. A typed client registers the
        // interface through a factory lambda, which Wolverine cannot call from
        // generated code — and it finds out only when the first message
        // arrives, so the failure lands on a request rather than at startup.
        services.AddScoped<ICompanyRegistryLookup, BrasilApiCompanyRegistryLookup>();

        services.AddHttpClient(BrasilApiCompanyRegistryLookup.HttpClientName, client =>
            {
                client.BaseAddress = new Uri(registry.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(registry.TotalTimeoutSeconds);
            })
            .AddResilienceHandler("registry", pipeline =>
            {
                pipeline.AddTimeout(TimeSpan.FromSeconds(registry.TotalTimeoutSeconds));

                pipeline.AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = registry.RetryAttempts,
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .Handle<HttpRequestException>()
                        .Handle<TimeoutRejectedException>()
                        .HandleResult(response => (int)response.StatusCode >= 500)
                });

                // Opens after sustained failure so the application stops
                // hammering a service that is already down.
                pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
                {
                    FailureRatio = 0.5,
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    MinimumThroughput = 5,
                    BreakDuration = TimeSpan.FromSeconds(15)
                });

                pipeline.AddTimeout(TimeSpan.FromSeconds(registry.TimeoutSeconds));
            });
    }
}
