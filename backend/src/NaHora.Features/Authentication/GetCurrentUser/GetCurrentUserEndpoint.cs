using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NaHora.Features.Abstractions;
using NaHora.Features.Common;
using Wolverine;

namespace NaHora.Features.Authentication.GetCurrentUser;

internal sealed class GetCurrentUserEndpoint : IEndpointGroup
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/api/v1/auth/me", HandleAsync)
            .RequireAuthorization()
            .WithName("GetCurrentUser")
            .WithTags("Authentication")
            .WithSummary("Retorna o perfil e as permissões do usuário autenticado.")
            .Produces<CurrentUserResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> HandleAsync(IMessageBus bus, CancellationToken cancellationToken)
    {
        var result = await bus.InvokeAsync<Shared.Results.Result<CurrentUserResponse>>(
            new GetCurrentUserQuery(),
            cancellationToken);

        return result.ToOk();
    }
}
