namespace NaHora.Domain.Attendance;

/// <summary>How the punch reached the server.</summary>
public enum PunchOrigin
{
    /// <summary>Recorded live. The official time is the server's own clock.</summary>
    Online = 1,

    /// <summary>
    /// Held on the device and sent later. The official time is the device's
    /// estimate, derived from a monotonic anchor — accepted, and marked as such.
    /// </summary>
    Offline = 2
}
