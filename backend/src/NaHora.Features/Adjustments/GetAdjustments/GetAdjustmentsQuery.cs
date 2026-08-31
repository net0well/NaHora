namespace NaHora.Features.Adjustments.GetAdjustments;

/// <summary>
/// Corrections the caller is entitled to see.
/// </summary>
/// <param name="PendingOnly">
/// The manager's queue. Without it, everything the caller may see, decided or
/// not.
/// </param>
public sealed record GetAdjustmentsQuery(bool PendingOnly);
