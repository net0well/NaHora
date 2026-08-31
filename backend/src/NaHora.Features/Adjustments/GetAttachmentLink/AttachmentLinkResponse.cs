namespace NaHora.Features.Adjustments.GetAttachmentLink;

/// <summary>
/// A link that works for a few minutes.
/// </summary>
/// <remarks>
/// No object key travels with it. The key is how storage is addressed, and a
/// client that knew one could try to build URLs for keys it was never given.
/// </remarks>
public sealed record AttachmentLinkResponse(Guid AdjustmentId, string Url);
