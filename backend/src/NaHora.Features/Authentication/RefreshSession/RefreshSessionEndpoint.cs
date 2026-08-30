using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NaHora.Features.Abstractions;
using NaHora.Features.Common;
using Wolverine;

namespace NaHora.Features.Authentication.RefreshSession;

internal sealed class RefreshSessionEndpoint : IEndpointGroup
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        // Anonymous by design: the access token has usually expired by the time
        // a client refreshes. The refresh token is the credential here.
        endpoints.MapPost("/api/v1/auth/refresh", HandleAsync)
            .AllowAnonymous()
            .WithName("RefreshSession")
            .WithTags("Authentication")
            .WithSummary("Rotaciona a sessão, emitindo um novo par de tokens.")
            .Produces<RefreshSessionResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> HandleAsync(
        RefreshSessionCommand command,
        IMessageBus bus,
        CancellationToken cancellationToken)
    {
        var result = await bus.InvokeAsync<Shared.Results.Result<RefreshSessionResponse>>(command, cancellationToken);

        return result.ToOk();
    }
}
