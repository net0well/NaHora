using FaceId.Features.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace FaceId.Features.System.GetSystemInfo;

internal sealed class GetSystemInfoEndpoint : IEndpointGroup
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/api/v1/system/info", HandleAsync)
            .WithName("GetSystemInfo")
            .WithTags("System")
            .WithSummary("Returns build and runtime information for diagnostics.")
            .Produces<GetSystemInfoResponse>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> HandleAsync(IMessageBus bus, CancellationToken cancellationToken)
    {
        var response = await bus.InvokeAsync<GetSystemInfoResponse>(new GetSystemInfoQuery(), cancellationToken);

        return TypedResults.Ok(response);
    }
}
