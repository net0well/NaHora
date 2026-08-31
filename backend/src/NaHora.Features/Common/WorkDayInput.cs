namespace NaHora.Features.Common;

/// <summary>
/// One day of a working week as it arrives on the wire.
/// </summary>
/// <remarks>
/// Times are strings in "HH:mm" rather than a structured type: it is what a
/// form field holds, and parsing here keeps a malformed value a validation
/// error instead of a deserialisation failure the client cannot read.
/// </remarks>
public sealed record WorkDayInput(
    int DayOfWeek,
    string? StartTime,
    string? LunchStartTime,
    string? LunchEndTime,
    string? EndTime);
