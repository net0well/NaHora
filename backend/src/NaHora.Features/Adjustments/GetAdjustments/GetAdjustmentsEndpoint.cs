using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NaHora.Domain.Authorization;
using NaHora.Features.Abstractions;
using NaHora.Features.Common;
using Wolverine;

namespace NaHora.Features.Adjustments.GetAdjustments;

internal sealed class GetAdjustmentsEndpoint : IEndpointGroup
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/api/v1/adjustments", HandleAsync)
            .RequireAuthorization(Permissions.ViewOwnAttendance)
            .WithName("GetAdjustments")
            .WithTags("Ajuste")
            .WithSummary("Solicitações de ajuste que o solicitante pode ver.")
            .WithDescription(
                "Gestor vê as da empresa; qualquer outro vê as próprias e nada mais. " +
                "Quem vê o quê é decidido no servidor, não por parâmetro — parâmetro " +
                "é coisa que o cliente muda.")
            .Produces<IReadOnlyList<AdjustmentResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> HandleAsync(
        IMessageBus bus,
        CancellationToken cancellationToken,
        bool pendingOnly = false)
    {
        ArgumentNullException.ThrowIfNull(bus);

        var result = await bus.InvokeAsync<Shared.Results.Result<IReadOnlyList<AdjustmentResponse>>>(
            new GetAdjustmentsQuery(pendingOnly),
            cancellationToken);

        return result.ToOk();
    }
}
