using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NaHora.Domain.Abstractions;
using NaHora.Domain.Adjustments;
using NaHora.Domain.Authorization;
using NaHora.Domain.Employees;
using NaHora.Domain.Errors;
using NaHora.Features.Abstractions;
using NaHora.Features.Common;

namespace NaHora.Features.Adjustments.GetAttachmentLink;

/// <summary>
/// Hands out a short-lived link to a justification attachment.
/// </summary>
/// <remarks>
/// <para>
/// A link rather than the bytes: the file goes straight from storage to the
/// person who asked, without passing through the API, and the API is not a file
/// server.
/// </para>
/// <para>
/// The link expires in minutes. These are somebody's medical records — a URL
/// that keeps working is a URL that gets forwarded.
/// </para>
/// </remarks>
internal sealed class GetAttachmentLinkEndpoint : IEndpointGroup
{
    public void Map(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/api/v1/adjustments/{adjustmentId:guid}/attachment", HandleAsync)
            .RequireAuthorization(Permissions.ViewOwnAttendance)
            .WithName("GetAttachmentLink")
            .WithTags("Ajuste")
            .WithSummary("Link temporário para o anexo da solicitação.")
            .WithDescription(
                "Vale por poucos minutos. São documentos pessoais: link que continua " +
                "funcionando é link que acaba encaminhado. Só o solicitante, o dono " +
                "do registro e gestores da mesma empresa recebem.")
            .Produces<AttachmentLinkResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        Guid adjustmentId,
        ICurrentUser currentUser,
        IPunchAdjustmentRepository adjustments,
        IEmployeeRepository employees,
        IAttachmentStorage storage,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(currentUser);
        ArgumentNullException.ThrowIfNull(adjustments);
        ArgumentNullException.ThrowIfNull(employees);
        ArgumentNullException.ThrowIfNull(storage);

        var companyId = currentUser.CompanyId;

        var adjustment = await adjustments.GetByIdAsync(companyId, adjustmentId, cancellationToken);
        if (adjustment is null)
        {
            return Shared.Results.Result.Failure(AdjustmentErrors.NotFound()).ToProblem();
        }

        var caller = await employees.GetByUserIdAsync(companyId, currentUser.UserId, cancellationToken);

        var mayRead = adjustment.RequestedByUserId == currentUser.UserId
                      || currentUser.HasPermission(Permissions.AdjustAttendance)
                      || (caller is not null && caller.Id == adjustment.EmployeeId);

        // The same answer a stranger gets. Distinguishing "not yours" from "not
        // there" would confirm the record exists.
        if (!mayRead)
        {
            return Shared.Results.Result.Failure(AdjustmentErrors.NotFound()).ToProblem();
        }

        if (adjustment.AttachmentKey is not { } key)
        {
            return Shared.Results.Result.Failure(AdjustmentErrors.AttachmentMissing()).ToProblem();
        }

        var url = await storage.GetTemporaryUrlAsync(key, cancellationToken);

        return TypedResults.Ok(new AttachmentLinkResponse(adjustment.Id, url.ToString()));
    }
}
