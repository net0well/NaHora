using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NaHora.Domain.Authorization;
using NaHora.Features.Abstractions;
using NaHora.Features.Common;
using Wolverine;

namespace NaHora.Features.Adjustments.RequestAdjustment;

/// <remarks>
/// Requires only <see cref="Permissions.ViewOwnAttendance"/>: asking about your
/// own record is not the same act as deciding, and the handler is what refuses
/// somebody else's.
/// </remarks>
internal sealed class RequestAdjustmentEndpoint : IEndpointGroup
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPost("/api/v1/adjustments", HandleAsync)
            .RequireAuthorization(Permissions.ViewOwnAttendance)
            .WithName("RequestAdjustment")
            .WithTags("Ajuste")
            .WithSummary("Solicita a correção de um registro de ponto.")
            .WithDescription(
                "Com punchId corrige uma marcação existente; sem ele inclui uma que " +
                "não houve. O funcionário pede sobre o próprio registro e o pedido " +
                "fica pendente; o gestor pede sobre o de outra pessoa e já vale. " +
                "A marcação original nunca é alterada.")
            .Produces<AdjustmentResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> HandleAsync(
        RequestAdjustmentCommand command,
        IMessageBus bus,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(bus);

        var result = await bus.InvokeAsync<Shared.Results.Result<AdjustmentResponse>>(
            command,
            cancellationToken);

        return result.IsFailure
            ? result.ToOk()
            : result.ToCreated($"/api/v1/adjustments/{result.Value.Id}");
    }
}
