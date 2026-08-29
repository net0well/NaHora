using FaceId.Api.Diagnostics;
using FaceId.Api.OpenApi;
using FaceId.Features;
using FaceId.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using Wolverine;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "FaceId"));

builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddOpenApi(options => options.AddDocumentTransformer<OpenApiDocumentInfoTransformer>());

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>(
        name: "postgresql",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready"]);

builder.Host.UseWolverine(options =>
{
    options.Discovery.IncludeAssembly(typeof(FeaturesServiceRegistration).Assembly);

    // Local-only for now. The RabbitMQ transport and the EF Core outbox are
    // wired when the first integration event exists, not before.
    options.Policies.AutoApplyTransactions();
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    // The document is produced by the built-in generator; Swashbuckle supplies
    // only the interface. The deprecated AddSwaggerGen path is not used.
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
