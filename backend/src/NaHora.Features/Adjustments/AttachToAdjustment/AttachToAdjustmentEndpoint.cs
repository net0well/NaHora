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

namespace NaHora.Features.Adjustments.AttachToAdjustment;

/// <summary>
/// Attaches the document that backs a correction.
/// </summary>
/// <remarks>
/// <para>
/// A separate request from the correction itself, and deliberately so: the
/// treatment is the record with legal weight and has to be stored even when an
/// upload fails on a phone with one bar of signal.
/// </para>
/// <para>
/// Handled in the endpoint rather than through the bus. A multipart body is a
/// stream, and putting a file through a message handler means either buffering
/// it in memory or serialising it — both worse than reading it once, here.
/// </para>
/// </remarks>
internal sealed class AttachToAdjustmentEndpoint : IEndpointGroup
{
    /// <summary>
    /// What a phone camera produces, with room to spare.
    /// </summary>
    /// <remarks>
    /// A cap rather than none: without one, a single request can fill the disk,
    /// and "the server ran out of space" is a worse failure than "that photo is
    /// too large".
    /// </remarks>
    private const long MaximumBytes = 10 * 1024 * 1024;

    private static readonly string[] AcceptedTypes =
    [
        "image/jpeg", "image/png", "image/heic", "application/pdf"
    ];

    public void Map(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPost("/api/v1/adjustments/{adjustmentId:guid}/attachment", HandleAsync)
            .RequireAuthorization(Permissions.ViewOwnAttendance)
            .DisableAntiforgery()
            .WithName("AttachToAdjustment")
            .WithTags("Ajuste")
            .WithSummary("Anexa o documento que embasa a solicitação.")
            .WithDescription(
                "Requisição separada da solicitação de propósito: o tratamento é o " +
                "registro com peso legal e precisa existir mesmo que o envio da foto " +
                "falhe. Aceita imagem ou PDF, até 10 MB.")
            .Produces<AttachmentResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        Guid adjustmentId,
        IFormFile file,
        ICurrentUser currentUser,
        IPunchAdjustmentRepository adjustments,
        IEmployeeRepository employees,
        IAttachmentStorage storage,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(currentUser);
        ArgumentNullException.ThrowIfNull(adjustments);
        ArgumentNullException.ThrowIfNull(employees);
        ArgumentNullException.ThrowIfNull(storage);
        ArgumentNullException.ThrowIfNull(unitOfWork);

        if (file is null || file.Length == 0)
        {
            return Shared.Results.Result.Failure(AdjustmentErrors.AttachmentKeyRequired()).ToProblem();
        }

        if (file.Length > MaximumBytes)
        {
            return Shared.Results.Result.Failure(AdjustmentErrors.AttachmentTooLarge(MaximumBytes)).ToProblem();
        }

        if (!AcceptedTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            return Shared.Results.Result
                .Failure(AdjustmentErrors.AttachmentTypeNotAccepted(file.ContentType))
                .ToProblem();
        }

        var companyId = currentUser.CompanyId;

        var adjustment = await adjustments.GetByIdAsync(companyId, adjustmentId, cancellationToken);
        if (adjustment is null)
        {
            return Shared.Results.Result.Failure(AdjustmentErrors.NotFound()).ToProblem();
        }

        // Whoever asked may attach to it; a manager may attach to any in their
        // company. Anybody else gets the same answer as a stranger.
        var caller = await employees.GetByUserIdAsync(companyId, currentUser.UserId, cancellationToken);
        var mayAttach = adjustment.RequestedByUserId == currentUser.UserId
                        || currentUser.HasPermission(Permissions.AdjustAttendance)
                        || (caller is not null && caller.Id == adjustment.EmployeeId);

        if (!mayAttach)
        {
            return Shared.Results.Result.Failure(AdjustmentErrors.NotFound()).ToProblem();
        }

        await using var content = file.OpenReadStream();

        var objectKey = await storage.SaveAsync(
            companyId, adjustment.Id, content, file.ContentType, cancellationToken);

        var attached = adjustment.Attach(objectKey);
        if (attached.IsFailure)
        {
            return attached.ToProblem();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(new AttachmentResponse(adjustment.Id, true));
    }
}
