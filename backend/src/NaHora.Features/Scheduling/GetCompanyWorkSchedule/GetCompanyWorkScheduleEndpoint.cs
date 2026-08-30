using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NaHora.Features.Abstractions;
using NaHora.Features.Common;
using Wolverine;

namespace NaHora.Features.Scheduling.GetCompanyWorkSchedule;

internal sealed class GetCompanyWorkScheduleEndpoint : IEndpointGroup
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/api/v1/companies/me/schedule", HandleAsync)
            .RequireAuthorization()
            .WithName("GetCompanyWorkSchedule")
            .WithTags("Jornada")
            .WithSummary("Jornada padrão da empresa.")
            .WithDescription(
                "Quando ainda não foi definida, responde a semana sugerida marcada " +
                "como não salva — um formulário em branco é pior ponto de partida " +
                "que o horário que quase todo escritório usa.")
            .Produces<CompanyWorkScheduleResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> HandleAsync(IMessageBus bus, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(bus);

        var result = await bus.InvokeAsync<Shared.Results.Result<CompanyWorkScheduleResponse>>(
            new GetCompanyWorkScheduleQuery(),
            cancellationToken);

        return result.ToOk();
    }
}
