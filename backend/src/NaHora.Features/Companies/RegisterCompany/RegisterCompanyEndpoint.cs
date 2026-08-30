using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NaHora.Features.Abstractions;
using NaHora.Features.Common;
using Wolverine;

namespace NaHora.Features.Companies.RegisterCompany;

/// <remarks>
/// Anonymous because there is nobody to authenticate as before the first
/// tenant exists. It is an operator action performed through the API — the app
/// has no sign-up screen and is not meant to grow one.
/// </remarks>
internal sealed class RegisterCompanyEndpoint : IEndpointGroup
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPost("/api/v1/companies", HandleAsync)
            .AllowAnonymous()

            // Anonymous and creating a tenant, so the limit is the only thing
            // between this route and somebody filling the table.
            .RequireRateLimiting(RateLimitPolicies.CompanyRegistration)
            .WithName("RegisterCompany")
            .WithTags("Empresa")
            .WithSummary("Cadastra uma empresa e seu primeiro administrador.")
            .WithDescription(
                "Provisiona o tenant inteiro em uma transação: sem conta, a empresa " +
                "seria uma linha que ninguém alcança. O CNPJ é único no sistema todo, " +
                "e é conferido na Receita — mas indisponibilidade da consulta não " +
                "impede o cadastro, apenas deixa a verificação pendente.")
            .Produces<RegisterCompanyResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        RegisterCompanyCommand command,
        IMessageBus bus,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(bus);

        var result = await bus.InvokeAsync<Shared.Results.Result<RegisterCompanyResponse>>(
            command,
            cancellationToken);

        return result.IsFailure
            ? result.ToOk()
            : result.ToCreated($"/api/v1/companies/{result.Value.CompanyId}");
    }
}
