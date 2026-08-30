using NaHora.Features.Attendance.GetMyPunchHistory;

namespace NaHora.UnitTests.Attendance;

/// <summary>
/// Paging values the client sent, and what the server actually uses.
/// </summary>
/// <remarks>
/// Nothing here rejects. A client asking for page zero or ten thousand rows is
/// almost always a bug in the client, and answering it with the nearest sane
/// page keeps the employee's own record reachable while that bug is fixed.
/// </remarks>
public sealed class GetMyPunchHistoryQueryTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Page_BelowTheFirst_FallsBackToTheFirst(int page)
    {
        // Arrange & Act
        var query = new GetMyPunchHistoryQuery(page, GetMyPunchHistoryQuery.DefaultPageSize);

        // Assert
        Assert.Equal(GetMyPunchHistoryQuery.DefaultPage, query.NormalisedPage);
        Assert.Equal(0, query.Skip);
    }

    [Fact]
    public void PageSize_AboveTheCeiling_IsClampedRatherThanRejected()
    {
        // Arrange & Act
        var query = new GetMyPunchHistoryQuery(1, 10_000);

        // Assert
        Assert.Equal(GetMyPunchHistoryQuery.MaxPageSize, query.NormalisedPageSize);
        Assert.Equal(GetMyPunchHistoryQuery.MaxPageSize, query.Take);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void PageSize_BelowOne_FallsBackToTheDefault(int pageSize)
    {
        // Arrange & Act
        var query = new GetMyPunchHistoryQuery(1, pageSize);

        // Assert
        Assert.Equal(GetMyPunchHistoryQuery.DefaultPageSize, query.NormalisedPageSize);
    }

    [Fact]
    public void Skip_CountsWholePages_FromTheNormalisedValues()
    {
        // Arrange & Act — page 3 of 20 starts after the first 40 rows
        var query = new GetMyPunchHistoryQuery(3, 20);

        // Assert
        Assert.Equal(40, query.Skip);
        Assert.Equal(20, query.Take);
    }
}
