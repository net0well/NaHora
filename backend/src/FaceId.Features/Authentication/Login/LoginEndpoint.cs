using FaceId.Features.Abstractions;
using FaceId.Features.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace FaceId.Features.Authentication.Login;

internal sealed class LoginEndpoint : IEndpointGroup
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPost("/api/v1/auth/login", HandleAsync)
            .AllowAnonymous()
            .WithName("Login")
            .WithTags("Authentication")
            .WithSummary("Autentica por CPF e senha e estabelece uma sessão.")
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> HandleAsync(
        LoginCommand command,
        IMessageBus bus,
        CancellationToken cancellationToken)
    {
        var result = await bus.InvokeAsync<Shared.Results.Result<LoginResponse>>(command, cancellationToken);

        return result.ToOk();
    }
}
