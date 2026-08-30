namespace NaHora.Features.Attendance.RegisterPunch;

/// <summary>Where the device says the punch happened.</summary>
public sealed record PunchLocationInput(
    double Latitude,
    double Longitude,
    double AccuracyMeters,
    DateTimeOffset CapturedAt);
