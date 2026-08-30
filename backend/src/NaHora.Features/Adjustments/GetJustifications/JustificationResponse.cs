namespace NaHora.Features.Adjustments.GetJustifications;

/// <param name="RequiresNotes">
/// True only for the catch-all. The app must refuse to submit without text when
/// this is set, otherwise "Outro" becomes the way to avoid choosing.
/// </param>
public sealed record JustificationResponse(
    Guid Id,
    string Code,
    string Description,
    bool RequiresNotes,
    int DisplayOrder);
