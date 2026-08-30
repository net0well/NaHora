namespace NaHora.Features.Employees.GetMyProfile;

/// <summary>
/// The caller's own employment record.
/// </summary>
/// <remarks>
/// Carries no identifier, for the same reason the company query does not: an
/// employee identifier in the request would make reading a colleague's profile
/// a matter of changing a number.
/// </remarks>
public sealed record GetMyProfileQuery;
