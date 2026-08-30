namespace NaHora.Features.Attendance.RegisterPunch;

/// <summary>
/// Present only when the punch waited in the device queue.
/// </summary>
/// <remarks>
/// <paramref name="OccurredAtEstimate"/> is the moment the app derived from its
/// monotonic anchor — the clock that counts since boot and is unaffected by
/// changing the date. It is accepted because the alternative is worse: stamping
/// arrival time would record a punch made at 07:00 as 14:00, when signal
/// returned.
///
/// The anchor fields travel with it so the estimate can be audited rather than
/// merely believed.
/// </remarks>
public sealed record OfflinePunchInput(
    DateTimeOffset OccurredAtEstimate,
    long MonotonicSinceBootMs,
    DateTimeOffset AnchorAt);
