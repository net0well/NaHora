using NaHora.Shared.Results;

namespace NaHora.Domain.Errors;

/// <summary>
/// Failures produced while treating a punch.
/// </summary>
/// <remarks>
/// None of these refuses a punch. They refuse a <b>correction</b> — the punch
/// itself is already recorded and immutable, and nothing here can undo that.
/// </remarks>
public static class AdjustmentErrors
{
    public static Error JustificationRequired() => Error.Validation(
        "JUSTIFICATION_REQUIRED",
        "A justification from the catalogue is required.",
        "justificationId");

    public static Error JustificationNotesRequired() => Error.Validation(
        "JUSTIFICATION_NOTES_REQUIRED",
        "This justification requires a written explanation.",
        "notes");

    public static Error JustificationNotFound() => Error.Validation(
        "JUSTIFICATION_NOT_FOUND",
        "That justification is not in the catalogue.",
        "justificationId");

    public static Error RequesterRequired() => Error.Validation(
        "ADJUSTMENT_REQUESTER_REQUIRED",
        "The person asking for the correction is required.",
        "requestedBy");

    public static Error OccurredAtInTheFuture() => Error.Validation(
        "ADJUSTMENT_OCCURRED_AT_IN_THE_FUTURE",
        "A correction cannot be for a time that has not happened yet.",
        "occurredAt");

    public static Error AttachmentKeyRequired() => Error.Validation(
        "ADJUSTMENT_ATTACHMENT_KEY_REQUIRED",
        "The attachment is missing.",
        "attachment");

    public static Error AlreadyDecided() => Error.Conflict(
        "ADJUSTMENT_ALREADY_DECIDED",
        "This correction has already been decided.");

    /// <summary>
    /// Approving a correction to your own timesheet is editing your own
    /// timesheet with extra steps.
    /// </summary>
    public static Error CannotDecideOwnRecord() => Error.Forbidden(
        "ADJUSTMENT_CANNOT_DECIDE_OWN_RECORD",
        "Somebody else has to decide a correction to your own record.");

    public static Error NotFound() => Error.NotFound(
        "ADJUSTMENT_NOT_FOUND",
        "No correction was found.");

    public static Error PunchNotFound() => Error.NotFound(
        "ADJUSTMENT_PUNCH_NOT_FOUND",
        "No punch was found to correct.");
}
