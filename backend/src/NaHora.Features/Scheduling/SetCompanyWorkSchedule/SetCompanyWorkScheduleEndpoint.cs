using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NaHora.Domain.Authorization;
using NaHora.Features.Abstractions;
using NaHora.Features.Common;
using Wolverine;

namespace NaHora.Features.Scheduling.SetCompanyWorkSchedule;

internal sealed class SetCompanyWorkScheduleEndpoint : IEndpointGroup
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPut("/api/v1/companies/me/schedule", HandleAsync)
            .RequireAuthorization(Permissions.ManageSchedules)
            .WithName("SetCompanyWorkSchedule")
            .WithTags("Jornada")
            .WithSummary("Define a jornada padrão da empresa.")
            .WithDescription(
                "Os sete dias vêm sempre juntos. Uma edição parcial que deixasse " +
                "sábado de fora o transformaria em dia de trabalho sem ninguém " +
                "ter pedido. Mudar a jornada não reinterpreta o passado.")
            .Produces<CompanyWorkScheduleResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> HandleAsync(
        SetCompanyWorkScheduleCommand command,
        IMessageBus bus,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(bus);

        var result = await bus.InvokeAsync<Shared.Results.Result<CompanyWorkScheduleResponse>>(
            command,
            cancellationToken);

        return result.ToOk();
    }
}
