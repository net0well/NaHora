namespace NaHora.Features.Attendance.RegisterPunch;

/// <summary>
/// A request to record a punch.
/// </summary>
/// <remarks>
/// Note what is absent: the time. The client never sends the moment that
/// counts, because a client that could would let an employee punch early by
/// changing the phone clock.
///
/// <paramref name="Offline"/> is the one exception, and it is not a
/// contradiction: a punch that waited in the device queue has no server moment
/// to borrow, so the estimate is accepted and marked as an estimate.
/// </remarks>
public sealed record RegisterPunchCommand(
    string ClientId,
    PunchLocationInput? Location,
    PunchDeviceInput? Device,
    OfflinePunchInput? Offline);
