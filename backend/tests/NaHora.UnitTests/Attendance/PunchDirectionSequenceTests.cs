using NaHora.Domain.Attendance;

namespace NaHora.UnitTests.Attendance;

/// <summary>
/// Which boundary of the day the next punch marks.
/// </summary>
/// <remarks>
/// Pinned here because two callers depend on it agreeing: the handler that
/// records a punch, and the screen that tells the employee what the next one
/// will be. A drift between them shows up as the app promising one thing and
/// the record saying another.
/// </remarks>
public sealed class PunchDirectionSequenceTests
{
    [Theory]
    [InlineData(0, PunchDirection.Entry)]
    [InlineData(1, PunchDirection.BreakStart)]
    [InlineData(2, PunchDirection.BreakEnd)]
    [InlineData(3, PunchDirection.Exit)]
    public void NextAfter_FollowsTheWorkingDay(int recorded, PunchDirection expected)
    {
        // Arrange & Act
        var direction = PunchDirectionSequence.NextAfter(recorded);

        // Assert
        Assert.Equal(expected, direction);
    }

    [Theory]
    [InlineData(4, PunchDirection.Entry)]
    [InlineData(5, PunchDirection.BreakStart)]
    [InlineData(7, PunchDirection.Exit)]
    public void NextAfter_StartsANewCycle_ForASplitShift(int recorded, PunchDirection expected)
    {
        // Arrange & Act — a second shift the same day is ordinary in Brazil,
        // not an edge case
        var direction = PunchDirectionSequence.NextAfter(recorded);

        // Assert
        Assert.Equal(expected, direction);
    }
}
