namespace NaHora.Domain.Adjustments;

/// <summary>
/// What a treatment does to the record.
/// </summary>
/// <remarks>
/// Neither one edits a punch. A correction points at an existing record and
/// says which time should count instead; an inclusion adds a time for a punch
/// that was never made. The original, when there is one, stays exactly as it
/// was recorded — it is the row that goes signed into the AFD.
/// </remarks>
public enum PunchAdjustmentKind
{
    /// <summary>The punch exists and its effective time should be another.</summary>
    TimeChange = 0,

    /// <summary>No punch was made, and one should count as if it had been.</summary>
    Inclusion = 1
}
