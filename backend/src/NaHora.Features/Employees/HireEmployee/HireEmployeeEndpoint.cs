using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NaHora.Domain.Authorization;
using NaHora.Features.Abstractions;
using NaHora.Features.Common;
using Wolverine;

namespace NaHora.Features.Employees.HireEmployee;

internal sealed class HireEmployeeEndpoint : IEndpointGroup
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPost("/api/v1/employees", HandleAsync)
            .RequireAuthorization(Permissions.ManageEmployees)
            .WithName("HireEmployee")
            .WithTags("Funcionário")
            .WithSummary("Cadastra um funcionário e a conta de acesso dele.")
            .WithDescription(
                "Cria o vínculo e o usuário na mesma transação. A senha é gerada e " +
                "enviada por e-mail ao funcionário — quem cadastra não a define nem " +
                "a vê. Sem PIS o cadastro é aceito e sinalizado: falta para o AFD, " +
                "não impede contratar nem marcar ponto.")
            .Produces<HireEmployeeResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> HandleAsync(
        HireEmployeeCommand command,
        IMessageBus bus,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(bus);

        var result = await bus.InvokeAsync<Shared.Results.Result<HireEmployeeResponse>>(
            command,
            cancellationToken);

        return result.IsFailure
            ? result.ToOk()
            : result.ToCreated($"/api/v1/employees/{result.Value.EmployeeId}");
    }
}
