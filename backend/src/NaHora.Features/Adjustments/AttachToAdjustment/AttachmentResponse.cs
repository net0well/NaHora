namespace NaHora.Features.Adjustments.AttachToAdjustment;

/// <summary>
/// Confirmation that the document was stored.
/// </summary>
/// <remarks>
/// Carries no key and no URL. The object is reached through its own authorised
/// route, so a confirmation cannot become a way to hand somebody a link.
/// </remarks>
public sealed record AttachmentResponse(Guid AdjustmentId, bool Attached);
