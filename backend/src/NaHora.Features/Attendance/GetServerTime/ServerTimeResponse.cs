namespace NaHora.Features.Attendance.GetServerTime;

/// <summary>
/// The official time, for a client that must not trust its own clock.
/// </summary>
/// <param name="ServerTimeUtc">
/// The instant the request was handled. What the app anchors to.
/// </param>
/// <param name="TimeZoneId">
/// The company's zone, so the app can render a local time without carrying its
/// own table of Brazilian offsets — and without disagreeing with the server on
/// which day a night-shift punch belongs to.
/// </param>
/// <param name="UtcOffsetSeconds">
/// The offset in force <em>now</em>. Sent as a number rather than derived on the
/// device, because a phone with an outdated timezone database would compute a
/// different one after a daylight-saving change.
/// </param>
public sealed record ServerTimeResponse(
    DateTimeOffset ServerTimeUtc,
    string TimeZoneId,
    int UtcOffsetSeconds);
