namespace NaHora.Features.Attendance.GetPunchLocation;

/// <summary>
/// Where one mark was made, for putting a pin on a map.
/// </summary>
/// <remarks>
/// One punch at a time, on purpose. The listings deliberately carry no
/// coordinates: answering them with positions would hand the device a map of
/// everywhere the person has been, and the device is the part of the system
/// most likely to be lost or taken. Asking for one position is a question about
/// one moment.
/// </remarks>
/// <param name="AccuracyMeters">
/// The radius the reading was good to. Drawn on the map as a circle rather than
/// dropped: a pin with no radius reads as certainty the GPS never had.
/// </param>
/// <param name="CapturedAt">
/// When the device read the position, which is not when the punch was recorded.
/// A gap between the two is what a stale fix looks like.
/// </param>
public sealed record PunchLocationResponse(
    Guid PunchId,
    double Latitude,
    double Longitude,
    double AccuracyMeters,
    DateTimeOffset CapturedAt,
    DateTimeOffset OccurredAt,
    int UtcOffsetSeconds);
