using FaceId.Api.Authentication;
using FaceId.Api.Diagnostics;
using FaceId.Api.Observability;
using FaceId.Api.OpenApi;
using FaceId.Api.RateLimiting;
using FaceId.Features;
using FaceId.Infrastructure;
using FaceId.Infrastructure.Configuration;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseFaceIdLogging();

builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddFaceIdAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddFaceIdRateLimiting(builder.Configuration);
builder.Services.AddFaceIdTelemetry(builder.Configuration);

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
        options.UseRabbitMq(factory =>
            {
                factory.HostName = messaging.Host;
                factory.Port = messaging.Port;
                factory.UserName = messaging.Username;
                factory.Password = messaging.Password;
                factory.VirtualHost = messaging.VirtualHost;
            })
            .AutoProvision()
            .BindExchange(messaging.Exchange)
            .ToQueue(messaging.Queue);

        // Publishing alone is half a pipeline: without a bound queue and a
        // listener the message leaves and nothing brings it back, so the
        // consumer never runs. The exchange fans out, the queue holds, and the
        // listener delivers to the handler.
        options.PublishAllMessages().ToRabbitExchange(messaging.Exchange);

        options.ListenToRabbitQueue(messaging.Queue)
            .UseDurableInbox();
    }
});

var app = builder.Build();

await DatabaseMigrator.MigrateAsync(app);

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
        options.SwaggerEndpoint("/openapi/v1.json", "FaceId API v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "FaceId API";
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
