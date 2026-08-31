using NaHora.Domain.Scheduling;

namespace NaHora.UnitTests.Scheduling;

/// <summary>
/// What a working day is allowed to look like.
/// </summary>
/// <remarks>
/// Every rejection here refuses a <b>definition</b>, never a punch. A schedule
/// says what was expected; Portaria 671 forbids turning that into a gate.
/// </remarks>
public sealed class WorkDayScheduleTests
{
    private static readonly TimeOnly Eight = new(8, 0);
    private static readonly TimeOnly Noon = new(12, 0);
    private static readonly TimeOnly One = new(13, 0);
    private static readonly TimeOnly Five = new(17, 0);

    [Fact]
    public void Create_WithLunch_ExpectsFourPunchesAndEightHours()
    {
        // Arrange & Act — 08:00 to 17:00 with an hour off is the ordinary day
        var result = WorkDaySchedule.Create(DayOfWeek.Monday, Eight, Noon, One, Five);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ExpectedPunches.ShouldBe(4, "entry, lunch out, lunch in, exit");
        result.Value.ExpectedMinutes.ShouldBe(480);
        result.Value.LunchMinutes.ShouldBe(60);
    }

    [Fact]
    public void Create_WithoutLunch_ExpectsTwoPunches()
    {
        // Arrange & Act — a short shift with no break
        var result = WorkDaySchedule.Create(DayOfWeek.Saturday, Eight, null, null, Noon);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ExpectedPunches.ShouldBe(2);
        result.Value.ExpectedMinutes.ShouldBe(240);
    }

    [Fact]
    public void Create_WithNothingFilledIn_IsADayOff_NotAnError()
    {
        // Arrange & Act — a weekend is a legitimate answer, not a broken form
        var result = WorkDaySchedule.Create(DayOfWeek.Sunday, null, null, null, null);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.IsWorkday.ShouldBeFalse();
        result.Value.ExpectedPunches.ShouldBe(0);
        result.Value.ExpectedMinutes.ShouldBe(0);
    }

    [Fact]
    public void Create_WithOnlyHalfOfTheBreak_IsRejected()
    {
        // Arrange & Act — accepting it would leave the calculation guessing how
        // long the break lasted
        var result = WorkDaySchedule.Create(DayOfWeek.Monday, Eight, Noon, null, Five);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldContain(error => error.Code == "SCHEDULE_LUNCH_INCOMPLETE");
    }

    [Fact]
    public void Create_WithOnlyAStartTime_IsRejected()
    {
        // Arrange & Act
        var result = WorkDaySchedule.Create(DayOfWeek.Monday, Eight, null, null, null);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldContain(error => error.Code == "SCHEDULE_DAY_INCOMPLETE");
    }

    [Fact]
    public void Create_CrossingMidnight_IsRejectedRatherThanProducingANegativeDay()
    {
        // Arrange & Act — a night shift is real and this build does not model it
        var result = WorkDaySchedule.Create(DayOfWeek.Monday, new TimeOnly(22, 0), null, null, new TimeOnly(6, 0));

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldContain(error => error.Code == "SCHEDULE_OVERNIGHT_NOT_SUPPORTED");
    }

    [Fact]
    public void Create_WithTheBreakOutsideTheWorkday_IsRejected()
    {
        // Arrange & Act — lunch at 19:00 on a day that ends at 17:00
        var result = WorkDaySchedule.Create(
            DayOfWeek.Monday, Eight, new TimeOnly(19, 0), new TimeOnly(20, 0), Five);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldContain(error => error.Code == "SCHEDULE_LUNCH_OUTSIDE_WORKDAY");
    }

    [Fact]
    public void Create_WithTheBreakEndingBeforeItStarts_IsRejected()
    {
        // Arrange & Act
        var result = WorkDaySchedule.Create(DayOfWeek.Monday, Eight, One, Noon, Five);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldContain(error => error.Code == "SCHEDULE_LUNCH_OUT_OF_ORDER");
    }

    [Fact]
    public void Create_PointsTheErrorAtTheDayThatIsWrong()
    {
        // Arrange & Act — so a form highlights Wednesday, not the whole week
        var result = WorkDaySchedule.Create(DayOfWeek.Wednesday, Eight, Noon, null, Five);

        // Assert
        result.Errors[0].Field.ShouldBe("days[3]");
    }

    [Theory]
    [InlineData(8, 12, 13, 17, "08:00 às 17:00, com 1h de intervalo")]
    [InlineData(8, 0, 0, 12, "08:00 às 12:00")]
    public void Describe_ReadsTheWayAPersonWouldSayIt(
        int start, int lunchStart, int lunchEnd, int end, string expected)
    {
        // Arrange
        var hasLunch = lunchStart != 0;

        var day = WorkDaySchedule.Create(
            DayOfWeek.Monday,
            new TimeOnly(start, 0),
            hasLunch ? new TimeOnly(lunchStart, 0) : null,
            hasLunch ? new TimeOnly(lunchEnd, 0) : null,
            new TimeOnly(end, 0)).Value;

        // Act & Assert
        day.Describe().ShouldBe(expected);
    }

    [Fact]
    public void Describe_ADayOff_SaysSo()
    {
        // Arrange & Act & Assert
        WorkDaySchedule.Off(DayOfWeek.Sunday).Describe().ShouldBe("Sem expediente");
    }

    [Fact]
    public void TwoDaysWithTheSameHours_AreEqual()
    {
        // Arrange — a value object has no identity of its own
        var first = WorkDaySchedule.Create(DayOfWeek.Monday, Eight, Noon, One, Five).Value;
        var second = WorkDaySchedule.Create(DayOfWeek.Monday, Eight, Noon, One, Five).Value;

        // Act & Assert
        first.ShouldBe(second);
    }
}
