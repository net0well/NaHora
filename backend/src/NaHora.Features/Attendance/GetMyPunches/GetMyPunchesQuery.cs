namespace NaHora.Features.Attendance.GetMyPunches;

/// <summary>
/// The signed-in employee's punches for one working day.
/// </summary>
/// <remarks>
/// Carries no employee: it is always the caller's own. Accepting one would let
/// anybody with the permission read a colleague's day by changing a number.
/// </remarks>
/// <param name="LocalDate">
/// The day, in the company timezone. Null means today — resolved on the server,
/// because the device's idea of the date is exactly what this product does not
/// trust.
/// </param>
public sealed record GetMyPunchesQuery(DateOnly? LocalDate);
