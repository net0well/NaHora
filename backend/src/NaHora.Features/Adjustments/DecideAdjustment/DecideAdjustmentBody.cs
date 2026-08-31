namespace NaHora.Features.Adjustments.DecideAdjustment;

/// <summary>
/// The body, without the identifier.
/// </summary>
/// <remarks>
/// The adjustment comes from the route, so repeating it in the payload would
/// create two sources for one value and a question about which wins.
/// </remarks>
public sealed record DecideAdjustmentBody(bool Approve, string? Notes);
