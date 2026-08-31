using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NaHora.Features.Abstractions;
using NaHora.Features.Common;
using Wolverine;

namespace NaHora.Features.Scheduling.GetMyWorkSchedule;

internal sealed class GetMyWorkScheduleEndpoint : IEndpointGroup
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/api/v1/schedules/me", HandleAsync)
            .RequireAuthorization()
            .WithName("GetMyWorkSchedule")
            .WithTags("Jornada")
            .WithSummary("Jornada esperada do próprio funcionário.")
            .WithDescription(
                "A do funcionário quando existe, senão a padrão da empresa. É o que " +
                "a tela de marcação exibe acima do botão. Informação, nunca trava: " +
                "nada aqui impede uma marcação.")
            .Produces<GetMyWorkScheduleResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> HandleAsync(IMessageBus bus, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(bus);

        var result = await bus.InvokeAsync<Shared.Results.Result<GetMyWorkScheduleResponse>>(
            new GetMyWorkScheduleQuery(),
            cancellationToken);

        return result.ToOk();
    }
}
