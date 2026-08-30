namespace NaHora.Domain.Attendance;

/// <summary>
/// What the device told us about itself when the punch was made.
/// </summary>
/// <remarks>
/// None of this blocks a punch. It exists so a manager reviewing a suspicious
/// record has the facts instead of a hunch, which is the whole compromise of
/// dropping biometrics: the system stops deciding and starts evidencing.
/// </remarks>
public sealed record DeviceIntegrity
{
    private DeviceIntegrity(
        string deviceId,
        bool locationWasMocked,
        DateTimeOffset deviceClockAt,
        int clockSkewSeconds)
    {
        DeviceId = deviceId;
        LocationWasMocked = locationWasMocked;
        DeviceClockAt = deviceClockAt;
        ClockSkewSeconds = clockSkewSeconds;
    }

    /// <summary>
    /// Identifier of the installation. Not a person, and not stable across a
    /// reinstall — enough to notice that four employees punch from one phone,
    /// not enough to be a credential.
    /// </summary>
    public string DeviceId { get; }

    /// <summary>
    /// The platform reported the position as coming from a mock provider.
    /// </summary>
    /// <remarks>
    /// Catches the easy path — a fake GPS app takes two minutes to install. A
    /// rooted device with the right tooling gets past it. A barrier against the
    /// opportunist, not the determined.
    /// </remarks>
    public bool LocationWasMocked { get; }

    /// <summary>What the device believed the time was.</summary>
    public DateTimeOffset DeviceClockAt { get; }

    /// <summary>
    /// How far the device clock sat from the time the punch was recorded at.
    /// </summary>
    /// <remarks>
    /// The signal that someone moved their clock forward to punch early. Stored
    /// signed: negative means the device was behind, which is usually a phone
    /// that has been offline a long time rather than anything deliberate.
    /// </remarks>
    public int ClockSkewSeconds { get; }

    public static DeviceIntegrity Create(
        string deviceId,
        bool locationWasMocked,
        DateTimeOffset deviceClockAt,
        DateTimeOffset officialTime) =>
        new(
            string.IsNullOrWhiteSpace(deviceId) ? "unknown" : deviceId.Trim(),
            locationWasMocked,
            deviceClockAt,
            (int)Math.Round((deviceClockAt - officialTime).TotalSeconds));
}
