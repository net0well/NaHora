using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NaHora.Domain.Authorization;
using NaHora.Features.Abstractions;
using NaHora.Features.Common;
using Wolverine;

namespace NaHora.Features.Scheduling.SetEmployeeWorkSchedule;

internal sealed class SetEmployeeWorkScheduleEndpoint : IEndpointGroup
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPut("/api/v1/employees/{employeeId:guid}/schedule", HandleAsync)
            .RequireAuthorization(Permissions.ManageSchedules)
            .WithName("SetEmployeeWorkSchedule")
            .WithTags("Jornada")
            .WithSummary("Define a jornada de um funcionário.")
            .WithDescription(
                "Abre uma vigência nova e fecha a anterior no dia anterior ao início. " +
                "Editar a linha no lugar julgaria cada dia já trabalhado por um " +
                "horário que não valia na época.")
            .Produces<EmployeeWorkScheduleResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> HandleAsync(
        Guid employeeId,
        SetEmployeeWorkScheduleBody body,
        IMessageBus bus,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(bus);

        var result = await bus.InvokeAsync<Shared.Results.Result<EmployeeWorkScheduleResponse>>(
            new SetEmployeeWorkScheduleCommand(employeeId, body.EffectiveFrom, body.Days),
            cancellationToken);

        return result.ToOk();
    }
}
