using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NaHora.Domain.Authorization;
using NaHora.Features.Abstractions;
using NaHora.Features.Common;
using Wolverine;

namespace NaHora.Features.Attendance.RegisterPunch;

internal sealed class RegisterPunchEndpoint : IEndpointGroup
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPost("/api/v1/punches", HandleAsync)
            .RequireAuthorization(Permissions.RegisterAttendance)
            .WithName("RegisterPunch")
            .WithTags("Ponto")
            .WithSummary("Registra uma marcação de ponto.")
            .WithDescription(
                "O horário oficial é do servidor: o cliente não envia hora, exceto " +
                "quando a marcação saiu da fila offline, e nesse caso a estimativa " +
                "é aceita e sinalizada. Localização é obrigatória, mas nem imprecisão " +
                "nem GPS falso impedem o registro — Portaria 671, Art. 78.")
            .Produces<RegisterPunchResponse>(StatusCodes.Status201Created)
            .Produces<RegisterPunchResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> HandleAsync(
        RegisterPunchCommand command,
        IMessageBus bus,
        CancellationToken cancellationToken)
    {
        var result = await bus.InvokeAsync<Shared.Results.Result<RegisterPunchResponse>>(
            command,
            cancellationToken);

        if (result.IsFailure)
        {
            return result.ToOk();
        }

        // A repeat of a punch already recorded answers 200, not 201: nothing
        // was created this time, and a client that retried should be able to
        // tell the difference.
        return result.Value.AlreadyRegistered
            ? result.ToOk()
            : result.ToCreated($"/api/v1/punches/{result.Value.Id}");
    }
}
