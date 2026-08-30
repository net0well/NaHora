using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NaHora.Features.Abstractions;
using NaHora.Features.Common;
using Wolverine;

namespace NaHora.Features.Employees.GetMyProfile;

internal sealed class GetMyProfileEndpoint : IEndpointGroup
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/api/v1/employees/me", HandleAsync)
            .RequireAuthorization()
            .WithName("GetMyEmployeeProfile")
            .WithTags("Funcionário")
            .WithSummary("Ficha do próprio funcionário.")
            .WithDescription(
                "Resolvida pelo usuário do token. Traz também se a ficha está " +
                "exportável para o AFD, para o funcionário ver o que falta no " +
                "próprio registro em vez de descobrir quando ficar de fora da exportação.")
            .Produces<GetMyProfileResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> HandleAsync(IMessageBus bus, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(bus);

        var result = await bus.InvokeAsync<Shared.Results.Result<GetMyProfileResponse>>(
            new GetMyProfileQuery(),
            cancellationToken);

        return result.ToOk();
    }
}
