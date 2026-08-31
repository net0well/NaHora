namespace NaHora.Features.Attendance.GetPunchLocation;

/// <summary>
/// Where one mark was made.
/// </summary>
/// <remarks>
/// Whose mark it may be is decided from the token, never from a parameter. The
/// employee gets their own; somebody who may see the whole company gets any of
/// its marks.
/// </remarks>
public sealed record GetPunchLocationQuery(Guid PunchId);
