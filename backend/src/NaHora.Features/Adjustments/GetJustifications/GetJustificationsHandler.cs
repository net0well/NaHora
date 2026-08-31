using NaHora.Domain.Adjustments;
using NaHora.Shared.Results;

namespace NaHora.Features.Adjustments.GetJustifications;

/// <summary>
/// The catalogue the picker is built from.
/// </summary>
/// <remarks>
/// Served from the compiled catalogue rather than the table: they are the same
/// ten rows, seeded from this list, and a query for something that cannot
/// change is a round trip for nothing.
/// </remarks>
public static class GetJustificationsHandler
{
    public static Task<Result<IReadOnlyList<JustificationResponse>>> Handle(
        GetJustificationsQuery query,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<JustificationResponse> catalogue =
        [
            .. AdjustmentJustificationCatalogue.All
                .OrderBy(justification => justification.DisplayOrder)
                .Select(justification => new JustificationResponse(
                    justification.Id,
                    justification.Code,
                    justification.Description,
                    justification.RequiresNotes,
                    justification.DisplayOrder))
        ];

        return Task.FromResult(Result.Success(catalogue));
    }
}
