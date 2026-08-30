using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NaHora.Domain.Authorization;
using NaHora.Features.Abstractions;
using NaHora.Features.Common;
using Wolverine;

namespace NaHora.Features.Attendance.GetMyPunchHistory;

internal sealed class GetMyPunchHistoryEndpoint : IEndpointGroup
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/api/v1/punches/me/history", HandleAsync)
            .RequireAuthorization(Permissions.ViewOwnAttendance)
            .WithName("GetMyPunchHistory")
            .WithTags("Ponto")
            .WithSummary("Histórico de marcações do próprio funcionário.")
            .WithDescription(
                "Mais recentes primeiro, uma página por vez. Página e tamanho fora " +
                "da faixa são ajustados em vez de rejeitados — um cliente que pediu " +
                "demais recebe o teto, não um erro.")
            .Produces<GetMyPunchHistoryResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> HandleAsync(
        IMessageBus bus,
        CancellationToken cancellationToken,
        int page = GetMyPunchHistoryQuery.DefaultPage,
        int pageSize = GetMyPunchHistoryQuery.DefaultPageSize)
    {
        ArgumentNullException.ThrowIfNull(bus);

        var result = await bus.InvokeAsync<Shared.Results.Result<GetMyPunchHistoryResponse>>(
            new GetMyPunchHistoryQuery(page, pageSize),
            cancellationToken);

        return result.ToOk();
    }
}
