using FaceId.Features.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace FaceId.Features.System.GetSystemInfo;

/// <summary>
/// Reference slice: proves the endpoint to query to handler path end to end.
/// Everything the feature needs lives in this folder, and nothing outside it
/// depends on these types.
/// </summary>
public static class GetSystemInfo
{
    public sealed record Query;

    public sealed record Response(string Application, string Version, string Environment, DateTimeOffset ServerTimeUtc);

    /// <summary>
    /// Wolverine discovers handlers by type name, so the nested type has to be
    /// called Handler. The method signature supplies its own dependencies.
    /// </summary>
    public static class Handler
    {
        public static Response Handle(Query query, TimeProvider clock)
        {
            ArgumentNullException.ThrowIfNull(clock);

            var version = typeof(Handler).Assembly.GetName().Version?.ToString() ?? "unknown";
            var environment = global::System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

            return new Response("FaceId", version, environment, clock.GetUtcNow());
        }
    }

    internal sealed class Endpoint : IEndpointGroup
    {
        public void Map(IEndpointRouteBuilder endpoints)
        {
            ArgumentNullException.ThrowIfNull(endpoints);

            endpoints.MapGet("/api/v1/system/info", async (IMessageBus bus, CancellationToken cancellationToken) =>
                {
                    var response = await bus.InvokeAsync<Response>(new Query(), cancellationToken);
                    return Results.Ok(response);
                })
                .WithName("GetSystemInfo")
                .WithTags("System")
                .WithSummary("Returns build and runtime information for diagnostics.")
                .Produces<Response>(StatusCodes.Status200OK);
        }
    }
}
