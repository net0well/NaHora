using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace FaceId.Api.Observability;

public static class ObservabilityRegistration
{
    /// <summary>
    /// Structured logging in compact JSON, with sensitive values stripped by
    /// policy rather than by convention.
    /// </summary>
    public static IHostBuilder UseFaceIdLogging(this IHostBuilder host)
    {
        ArgumentNullException.ThrowIfNull(host);

        return host.UseSerilog((context, services, configuration) => configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithProperty("Application", FaceIdTelemetry.ServiceName)

            // Correlates a log line with the trace it belongs to, which is what
            // makes jumping between the two useful.
            .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
            .Destructure.With<SensitiveDataPolicy>()

            // The health probes fire every few seconds and say nothing. Left at
            // Information they would bury everything else.
            .Filter.ByExcluding(logEvent =>
                logEvent.Level < LogEventLevel.Warning
                && logEvent.Properties.TryGetValue("RequestPath", out var path)
                && path.ToString().Contains("/health", StringComparison.OrdinalIgnoreCase))

            .WriteTo.Console(new CompactJsonFormatter()));
    }

    /// <summary>
    /// Traces and metrics over OTLP.
    /// </summary>
    /// <remarks>
    /// The exporter only starts when an endpoint is configured, so a deployment
    /// without a collector runs without it instead of failing or retrying into
    /// nothing.
    /// </remarks>
    public static IServiceCollection AddFaceIdTelemetry(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var otlpEndpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
        var hasCollector = !string.IsNullOrWhiteSpace(otlpEndpoint);

        var telemetry = services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: FaceIdTelemetry.ServiceName,
                    serviceVersion: typeof(ObservabilityRegistration).Assembly.GetName().Version?.ToString()));

        telemetry.WithTracing(tracing =>
        {
            tracing
                .AddSource(FaceIdTelemetry.ServiceName)
                .AddAspNetCoreInstrumentation(options =>
                {
                    // Health probes would dominate the trace volume and answer
                    // no question anyone asks.
                    options.Filter = httpContext =>
                        !httpContext.Request.Path.StartsWithSegments("/health");

                    options.RecordException = true;
                })
                .AddHttpClientInstrumentation();

            if (hasCollector)
            {
                tracing.AddOtlpExporter();
            }
        });

        telemetry.WithMetrics(metrics =>
        {
            metrics
                .AddMeter(FaceIdTelemetry.ServiceName)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation();

            if (hasCollector)
            {
                metrics.AddOtlpExporter();
            }
        });

        return services;
    }
}
