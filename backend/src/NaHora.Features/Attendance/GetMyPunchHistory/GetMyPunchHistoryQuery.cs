namespace NaHora.Features.Attendance.GetMyPunchHistory;

/// <summary>
/// A page of the signed-in employee's punches, newest first.
/// </summary>
/// <remarks>
/// Always the caller's own, for the same reason the day query is: an employee
/// identifier in the request would make reading a colleague's history a matter
/// of changing a number.
/// </remarks>
public sealed record GetMyPunchHistoryQuery(int Page, int PageSize)
{
    /// <summary>The first page, when the client sends nothing.</summary>
    public const int DefaultPage = 1;

    public const int DefaultPageSize = 30;

    /// <summary>
    /// The most a single page may carry.
    /// </summary>
    /// <remarks>
    /// Clamped rather than rejected: a client asking for ten thousand rows gets
    /// the first hundred, not an error. The ceiling exists so one request
    /// cannot pull an employee's entire history into memory at once.
    /// </remarks>
    public const int MaxPageSize = 100;

    /// <summary>Rows to skip, from a page number the client may have made up.</summary>
    public int Skip => (NormalisedPage - 1) * NormalisedPageSize;

    public int Take => NormalisedPageSize;

    public int NormalisedPage => Page < 1 ? DefaultPage : Page;

    public int NormalisedPageSize => PageSize switch
    {
        < 1 => DefaultPageSize,
        > MaxPageSize => MaxPageSize,
        _ => PageSize
    };
}
