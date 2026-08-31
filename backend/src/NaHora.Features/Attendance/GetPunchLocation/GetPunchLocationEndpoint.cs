using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NaHora.Domain.Authorization;
using NaHora.Features.Abstractions;
using NaHora.Features.Common;
using Wolverine;

namespace NaHora.Features.Attendance.GetPunchLocation;

internal sealed class GetPunchLocationEndpoint : IEndpointGroup
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/api/v1/punches/{punchId:guid}/location", HandleAsync)
            .RequireAuthorization(Permissions.ViewOwnAttendance)
            .WithName("GetPunchLocation")
            .WithTags("Ponto")
            .WithSummary("Onde uma marcação foi feita.")
            .WithDescription(
                "A única rota que responde com coordenada, e sempre de uma marcação " +
                "só. As listagens não devolvem posição de propósito: um histórico de " +
                "onde a pessoa esteve não precisa trafegar para o aparelho dela. " +
                "Marcação de outra empresa, ou de outro funcionário para quem não " +
                "pode ver a empresa toda, responde 404 — dizer 403 já contaria que " +
                "ela existe.")
            .Produces<PunchLocationResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> HandleAsync(
        Guid punchId,
        IMessageBus bus,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(bus);

        var result = await bus.InvokeAsync<Shared.Results.Result<PunchLocationResponse>>(
            new GetPunchLocationQuery(punchId),
            cancellationToken);

        return result.ToOk();
    }
}
