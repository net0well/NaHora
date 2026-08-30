using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NaHora.Domain.Authorization;
using NaHora.Features.Abstractions;
using NaHora.Features.Common;
using Wolverine;

namespace NaHora.Features.Attendance.GetMyPunches;

internal sealed class GetMyPunchesEndpoint : IEndpointGroup
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/api/v1/punches/me", HandleAsync)
            .RequireAuthorization(Permissions.ViewOwnAttendance)
            .WithName("GetMyPunches")
            .WithTags("Ponto")
            .WithSummary("Marcações do próprio funcionário em um dia.")
            .WithDescription(
                "Sem o parâmetro de data responde o dia de hoje no fuso da empresa, " +
                "resolvido no servidor. Não devolve coordenadas: o histórico de onde " +
                "a pessoa esteve não precisa trafegar para o aparelho dela.")
            .Produces<GetMyPunchesResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> HandleAsync(
        DateOnly? localDate,
        IMessageBus bus,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(bus);

        var result = await bus.InvokeAsync<Shared.Results.Result<GetMyPunchesResponse>>(
            new GetMyPunchesQuery(localDate),
            cancellationToken);

        return result.ToOk();
    }
}
