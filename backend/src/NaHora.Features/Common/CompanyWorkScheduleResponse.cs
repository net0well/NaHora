namespace NaHora.Features.Common;

/// <summary>
/// A company's default working week.
/// </summary>
/// <remarks>
/// In Common because both the slice that reads it and the slice that writes it
/// answer with the same shape, and slices do not reference each other.
/// </remarks>
/// <param name="IsDefined">
/// False when nobody has set the hours yet, in which case the week returned is
/// the suggestion a form can start from rather than something already saved.
/// </param>
public sealed record CompanyWorkScheduleResponse(
    bool IsDefined,
    string TimeZoneId,
    IReadOnlyList<WorkDayResponse> Week);
