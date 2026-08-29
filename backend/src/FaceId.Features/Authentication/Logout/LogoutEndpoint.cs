using FaceId.Features.Abstractions;
using FaceId.Features.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace FaceId.Features.Authentication.Logout;

internal sealed class LogoutEndpoint : IEndpointGroup
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPost("/api/v1/auth/logout", HandleAsync)
            .RequireAuthorization()
            .WithName("Logout")
            .WithTags("Authentication")
            .WithSummary("Revoga a sessão atual, ou todas as sessões do usuário.")
            .Produces(StatusCodes.Status204NoContent);
    }

    private static async Task<IResult> HandleAsync(
        LogoutCommand command,
        IMessageBus bus,
        CancellationToken cancellationToken)
    {
        var result = await bus.InvokeAsync<Shared.Results.Result>(command, cancellationToken);

        return result.ToNoContent();
    }
}
