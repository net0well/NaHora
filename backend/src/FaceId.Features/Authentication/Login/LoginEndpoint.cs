using FaceId.Features.Abstractions;
using FaceId.Features.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
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
            .RequireRateLimiting(RateLimitPolicies.Login)
            .WithName("Login")
            .WithTags("Authentication")
            .WithSummary("Autentica por CPF e senha e estabelece uma sessão.")
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> HandleAsync(
        LoginCommand command,
        HttpContext httpContext,
        IMessageBus bus,
        CancellationToken cancellationToken)
    {
        // Taken from the connection, never from the payload: an address the
        // client chose is worse than none, because it looks trustworthy in an
        // audit trail.
        var enriched = command with
        {
            IpAddress = httpContext.Connection.RemoteIpAddress?.ToString()
        };

        var result = await bus.InvokeAsync<Shared.Results.Result<LoginResponse>>(enriched, cancellationToken);

        return result.ToOk();
    }
}
