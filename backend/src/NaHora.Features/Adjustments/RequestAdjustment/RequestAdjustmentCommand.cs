namespace NaHora.Features.Adjustments.RequestAdjustment;

/// <summary>
/// Asks for a time record to be corrected.
/// </summary>
/// <remarks>
/// <para>
/// One command for both shapes. With a <paramref name="PunchId"/> it corrects
/// an existing record; without one it asks for a punch that was never made to
/// count. The two share every rule that matters — justification, who may ask,
/// who may agree — and splitting them would duplicate all of it.
/// </para>
/// <para>
/// <paramref name="EmployeeId"/> is null when somebody asks about their own
/// record, and set only by a manager acting on somebody else's.
/// </para>
/// </remarks>
public sealed record RequestAdjustmentCommand(
    Guid? PunchId,
    Guid? EmployeeId,
    DateTimeOffset? OccurredAt,
    string? Direction,
    Guid JustificationId,
    string? Notes);
