using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NaHora.Features.Abstractions;
using NaHora.Features.Common;
using Wolverine;

namespace NaHora.Features.Adjustments.GetJustifications;

internal sealed class GetJustificationsEndpoint : IEndpointGroup
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/api/v1/justifications", HandleAsync)
            .RequireAuthorization()
            .WithName("GetJustifications")
            .WithTags("Ajuste")
            .WithSummary("Catálogo fixo de justificativas.")
            .WithDescription(
                "Não cadastrável pelo cliente. Justificativa livre vira campo vazio " +
                "ou 'ajuste', que não serve à fiscalização nem à auditoria interna.")
            .Produces<IReadOnlyList<JustificationResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> HandleAsync(IMessageBus bus, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(bus);

        var result = await bus.InvokeAsync<Shared.Results.Result<IReadOnlyList<JustificationResponse>>>(
            new GetJustificationsQuery(),
            cancellationToken);

        return result.ToOk();
    }
}
