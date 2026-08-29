using FaceId.Api.Authentication;
using FaceId.Api.Diagnostics;
using FaceId.Api.OpenApi;
using FaceId.Features;
using FaceId.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using Wolverine;
using Wolverine.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "FaceId"));

builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddFaceIdAuthentication(builder.Configuration, builder.Environment);

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

    // Teaches Wolverine about the DbContext. Without it the generated handler
    // code cannot inline DbContextOptions, which AddDbContext registers as an
    // opaque factory, and the runtime rejects the resulting service location.
    //
    // It is also what the transactional outbox will build on: an integration
    // event must be published only if the transaction that produced it
    // committed.
    options.UseEntityFrameworkCoreTransactions();

    // AutoApplyTransactions is deliberately absent: it enlists handlers in
    // Wolverine's durable envelope storage, which only exists once the outbox
    // is configured. Nothing is published yet, so handlers commit through
    // IUnitOfWork explicitly — which is also easier to follow than an
    // implicit transaction the reader has to know about.
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseSerilogRequestLogging();

// Order matters: authentication populates the principal that authorisation and
// the tenant filter both read.
app.UseAuthentication();
app.UseAuthorization();

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
