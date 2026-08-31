namespace NaHora.Features.Adjustments.DecideAdjustment;

/// <summary>
/// A manager's answer to a request.
/// </summary>
/// <param name="Approve">
/// True agrees and the adjusted time starts counting; false declines and the
/// request is kept, because deleting it would hide that somebody asked.
/// </param>
public sealed record DecideAdjustmentCommand(Guid AdjustmentId, bool Approve, string? Notes);
