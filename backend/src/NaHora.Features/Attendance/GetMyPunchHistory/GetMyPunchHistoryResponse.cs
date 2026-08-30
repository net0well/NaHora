namespace NaHora.Features.Attendance.GetMyPunchHistory;

/// <summary>
/// A page of history, with enough to know whether to ask for another.
/// </summary>
/// <param name="Page">The page actually served, which may differ from the one asked for.</param>
/// <param name="PageSize">The size actually served, clamped to the ceiling.</param>
/// <param name="TotalCount">Every punch the employee has, not just this page.</param>
/// <param name="HasMore">
/// Answered by the server rather than left to the client to derive. A client
/// computing it from the other three gets it wrong at the boundary, and the
/// symptom is a list that never stops loading.
/// </param>
public sealed record GetMyPunchHistoryResponse(
    int Page,
    int PageSize,
    int TotalCount,
    bool HasMore,
    IReadOnlyList<PunchHistoryEntryResponse> Entries);
