using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NaHora.Domain.Authorization;
using NaHora.Features.Abstractions;
using NaHora.Features.Common;
using Wolverine;

namespace NaHora.Features.Adjustments.DecideAdjustment;

internal sealed class DecideAdjustmentEndpoint : IEndpointGroup
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPost("/api/v1/adjustments/{adjustmentId:guid}/decision", HandleAsync)
            .RequireAuthorization(Permissions.AdjustAttendance)
            .WithName("DecideAdjustment")
            .WithTags("Ajuste")
            .WithSummary("Aprova ou recusa uma solicitação de ajuste.")
            .WithDescription(
                "Só a partir daqui o horário ajustado passa a valer. Ninguém decide " +
                "sobre o próprio registro — aprovar correção da própria folha é " +
                "editar a própria folha com passos a mais. Recusa também fica " +
                "guardada: apagar esconderia que alguém pediu.")
            .Produces<AdjustmentResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> HandleAsync(
        Guid adjustmentId,
        DecideAdjustmentBody body,
        IMessageBus bus,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(bus);

        var result = await bus.InvokeAsync<Shared.Results.Result<AdjustmentResponse>>(
            new DecideAdjustmentCommand(adjustmentId, body.Approve, body.Notes),
            cancellationToken);

        return result.ToOk();
    }
}
