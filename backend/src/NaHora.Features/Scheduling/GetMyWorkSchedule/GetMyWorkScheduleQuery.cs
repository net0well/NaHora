namespace NaHora.Features.Scheduling.GetMyWorkSchedule;

/// <summary>
/// The hours the signed-in person is expected to work.
/// </summary>
/// <remarks>
/// Carries no employee, like every other "me" query: an identifier the caller
/// could set would turn reading a colleague's schedule into changing a number.
/// </remarks>
public sealed record GetMyWorkScheduleQuery;
