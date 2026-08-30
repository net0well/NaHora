namespace NaHora.Features.Attendance.RegisterPunch;

/// <summary>
/// What the device reports about itself.
/// </summary>
/// <remarks>
/// Self-reported, and treated as such: a rooted device can lie about all of it.
/// It is evidence for a human reviewer, never an authorisation decision.
/// </remarks>
public sealed record PunchDeviceInput(
    string DeviceId,
    bool LocationWasMocked,
    DateTimeOffset DeviceClockAt);
