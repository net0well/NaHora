using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NaHora.Domain.Abstractions;
using NaHora.Domain.Companies;
using NaHora.Features.Abstractions;

namespace NaHora.Features.Attendance.GetServerTime;

/// <summary>
/// The official time.
/// </summary>
/// <remarks>
/// Exists because the app must never show the device clock. A phone running two
/// hours fast would display 16:32 while the punch it produces records 14:32, and
/// the employee would be right to think the system lied to them.
///
/// Answered directly rather than through the bus: there is no business rule
/// here, only the clock, and routing a timestamp through a message handler
/// would add latency to the one call whose accuracy is the point.
/// </remarks>
internal sealed class GetServerTimeEndpoint : IEndpointGroup
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/api/v1/time", HandleAsync)
            .RequireAuthorization()
            .WithName("GetServerTime")
            .WithTags("Ponto")
            .WithSummary("Hora oficial do servidor.")
            .WithDescription(
                "O app ancora seu relógio nesta resposta e nunca exibe a hora do " +
                "aparelho. Offline, a hora é derivada dessa âncora com o relógio " +
                "monotônico, que mexer na data do celular não afeta.")
            .Produces<ServerTimeResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    private static async Task<Ok<ServerTimeResponse>> HandleAsync(
        ICurrentUser currentUser,
        ICompanyRepository companies,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(currentUser);
        ArgumentNullException.ThrowIfNull(companies);

        var now = clock.GetUtcNow();

        var company = await companies.GetByIdAsync(currentUser.CompanyId, cancellationToken);
        var timeZoneId = company?.TimeZoneId ?? Company.DefaultTimeZoneId;

        return TypedResults.Ok(new ServerTimeResponse(
            now,
            timeZoneId,
            (int)ResolveOffset(timeZoneId, now).TotalSeconds));
    }

    /// <summary>
    /// The zone's offset at this instant.
    /// </summary>
    /// <remarks>
    /// Falls back to UTC rather than throwing when the identifier is unknown.
    /// A misconfigured timezone must not stop someone reading the clock, for the
    /// same reason nothing else in this system stops them punching.
    /// </remarks>
    private static TimeSpan ResolveOffset(string timeZoneId, DateTimeOffset instant) =>
        TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out var zone)
            ? zone.GetUtcOffset(instant)
            : TimeSpan.Zero;
}
