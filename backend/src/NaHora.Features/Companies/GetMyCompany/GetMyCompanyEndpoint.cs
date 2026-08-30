using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NaHora.Features.Abstractions;
using NaHora.Features.Common;
using Wolverine;

namespace NaHora.Features.Companies.GetMyCompany;

internal sealed class GetMyCompanyEndpoint : IEndpointGroup
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/api/v1/companies/me", HandleAsync)
            .RequireAuthorization()
            .WithName("GetMyCompany")
            .WithTags("Empresa")
            .WithSummary("Dados da empresa do usuário autenticado.")
            .WithDescription(
                "A empresa vem do token, nunca da requisição. Aberto a qualquer " +
                "usuário autenticado: o funcionário tem direito de saber qual pessoa " +
                "jurídica registra as horas dele.")
            .Produces<GetMyCompanyResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> HandleAsync(IMessageBus bus, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(bus);

        var result = await bus.InvokeAsync<Shared.Results.Result<GetMyCompanyResponse>>(
            new GetMyCompanyQuery(),
            cancellationToken);

        return result.ToOk();
    }
}
