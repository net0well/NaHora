using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NaHora.Api.Authentication;
using NaHora.Api.Development;
using NaHora.Api.Diagnostics;
using NaHora.Api.Observability;
using NaHora.Api.OpenApi;
using NaHora.Api.RateLimiting;
using NaHora.Domain.Abstractions;
using NaHora.Domain.RealTime;
using NaHora.Features;
using NaHora.Infrastructure;
using NaHora.Infrastructure.Configuration;
using Serilog;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseNaHoraLogging();

builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddNaHoraAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddNaHoraRateLimiting(builder.Configuration);
builder.Services.AddNaHoraTelemetry(builder.Configuration);

builder.Services.Configure<DevelopmentSeedOptions>(
    builder.Configuration.GetSection(DevelopmentSeedOptions.SectionName));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddOpenApi(options => options.AddDocumentTransformer<OpenApiDocumentInfoTransformer>());

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>(
        name: "postgresql",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready"]);

var messaging = builder.Configuration.GetSection(MessagingOptions.SectionName).Get<MessagingOptions>()
                ?? new MessagingOptions();

var databaseConnectionString =
    builder.Configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>()?.ConnectionString
    ?? throw new InvalidOperationException("Database connection string is required for the message store.");

builder.Host.UseWolverine(options =>
{
    options.Discovery.IncludeAssembly(typeof(FeaturesServiceRegistration).Assembly);

    // The envelope store. UseEntityFrameworkCoreTransactions only enlists the
    // DbContext in the transaction; without a store there is nowhere to durably
    // hold the outgoing message, and the handler fails at runtime.
    options.PersistMessagesWithPostgresql(databaseConnectionString);

    options.UseEntityFrameworkCoreTransactions();

    // Enrols every handler in a transaction that also covers the outbox write.
    // A message is therefore only sent if the change that produced it
    // committed — closing the dual-write problem this exists for.
    options.Policies.AutoApplyTransactions();

    if (messaging.Enabled)
    {
        var topology = messaging.Topology;
        var realTimeQueue = topology.ResolveRealTimeQueue();

        options.UseRabbitMq(factory =>
            {
                factory.HostName = messaging.Host;
                factory.Port = messaging.Port;
                factory.UserName = messaging.Username;
                factory.Password = messaging.Password;
                factory.VirtualHost = messaging.VirtualHost;
            })
            .AutoProvision()

            // Durable and shared. Instances compete for each message, because
            // one audit record per instance would be the same event recorded
            // three times.
            .BindExchange(topology.AuditExchange).ToQueue(topology.AuditQueue)

            // Private to this instance, so every instance receives every live
            // update and can reach the observers connected to it. This is the
            // fan-out, and it is why no shared backplane is needed.
            .BindExchange(topology.RealTimeExchange).ToQueue(realTimeQueue, queue =>
            {
                // Dies with the process. A crashed instance leaves no queue
                // behind slowly filling with messages nobody will ever read.
                queue.AutoDelete = true;
                queue.IsExclusive = true;
                queue.IsDurable = false;
            });

        // Routed by contract rather than by name: an event says which delivery
        // it needs by the marker it implements, so a rename cannot quietly
        // reroute it into the wrong queue.
        options.Publish(rule => rule.MessagesImplementing<IAuditableEvent>()
            .ToRabbitExchange(topology.AuditExchange));

        options.Publish(rule => rule.Message<RealTimeEvent>()
            .ToRabbitExchange(topology.RealTimeExchange));

        // The audit stream is worth retrying and worth remembering across a
        // restart, so it goes through the durable inbox.
        options.ListenToRabbitQueue(topology.AuditQueue)
            .UseDurableInbox();

        // The live stream is not. A retried update is a stale update, and
        // persisting one to survive a restart delivers it to observers who
        // disconnected when the process did.
        options.ListenToRabbitQueue(realTimeQueue, queue =>
        {
            queue.AutoDelete = true;
            queue.IsExclusive = true;
            queue.IsDurable = false;
        }).BufferedInMemory();
    }
});

var app = builder.Build();

await DatabaseMigrator.MigrateAsync(app);

// After the schema exists, and before the first request can ask for a
// login that would otherwise have no company to authenticate against.
await DevelopmentSeed.ApplyAsync(app);

app.UseExceptionHandler();

// Before the request log, so every line it writes already carries the
// correlation identifier.
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();

app.UseRateLimiter();

// Order matters: authentication populates the principal that authorisation and
// the tenant filter both read.
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    // The document comes from the built-in generator; Swashbuckle supplies only
    // the interface. The deprecated AddSwaggerGen path is not used.
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "NaHora API v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "NaHora API";
        options.DisplayRequestDuration();
    });
}

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = HealthCheckResponseWriter.WriteAsync
});

app.MapFeatureEndpoints();

await app.RunAsync().ConfigureAwait(false);

/// <summary>
/// Exposed so the integration test project can build a host with
/// WebApplicationFactory.
/// </summary>
public partial class Program;
