using NaHora.Domain.Adjustments;

namespace NaHora.Features.Common;

/// <summary>
/// A correction, as every screen that shows one reads it.
/// </summary>
/// <remarks>
/// In Common because the employee's list, the manager's queue and the decision
/// response all answer with it, and slices do not reference each other.
///
/// Both times travel. Spec 014 requires the original to stay visible next to
/// the value in force — a correction that hides what it replaced is not a
/// correction, it is an edit.
/// </remarks>
/// <param name="Counts">
/// Whether this is the time that now governs. Only an approved treatment does.
/// </param>
public sealed record AdjustmentResponse(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    Guid? PunchId,
    string Kind,
    string Status,
    bool Counts,
    string Direction,
    DateTimeOffset? OriginalOccurredAt,
    DateTimeOffset AdjustedOccurredAt,
    int UtcOffsetSeconds,
    string JustificationCode,
    string JustificationDescription,
    string? Notes,
    bool HasAttachment,
    DateTimeOffset RequestedAt,
    string RequestedByName,
    DateTimeOffset? DecidedAt,
    string? DecisionNotes)
{
    public static AdjustmentResponse From(
        PunchAdjustment adjustment,
        string employeeName,
        string requestedByName,
        int utcOffsetSeconds)
    {
        ArgumentNullException.ThrowIfNull(adjustment);

        var justification = AdjustmentJustificationCatalogue.All
            .FirstOrDefault(entry => entry.Id == adjustment.JustificationId);

        return new AdjustmentResponse(
            adjustment.Id,
            adjustment.EmployeeId,
            employeeName,
            adjustment.PunchId,
            adjustment.Kind.ToString(),
            adjustment.Status.ToString(),
            adjustment.Counts,
            adjustment.Direction.ToString(),
            adjustment.OriginalOccurredAt,
            adjustment.AdjustedOccurredAt,
            utcOffsetSeconds,
            justification?.Code ?? "UNKNOWN",
            justification?.Description ?? "Justificativa desconhecida",
            adjustment.Notes,

            // Whether one exists, never where it is. The object key is fetched
            // through its own authorised route, so a listing cannot become a
            // way to enumerate somebody's medical certificates.
            adjustment.AttachmentKey is not null,
            adjustment.RequestedAt,
            requestedByName,
            adjustment.DecidedAt,
            adjustment.DecisionNotes);
    }
}
