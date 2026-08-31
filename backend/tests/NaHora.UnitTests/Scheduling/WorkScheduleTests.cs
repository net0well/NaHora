using NaHora.Domain.Scheduling;

namespace NaHora.UnitTests.Scheduling;

/// <summary>
/// The company default, the per-employee override, and which of the two governs
/// a given day.
/// </summary>
public sealed class WorkScheduleTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 30, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid Company = Guid.CreateVersion7();
    private static readonly Guid Employee = Guid.CreateVersion7();

    [Fact]
    public void Default_IsMondayToFridayEightToFive()
    {
        // Arrange & Act — a new company has hours from its first day, so the
        // employee's screen never has a blank to apologise for
        var schedule = CompanyWorkSchedule.Default(Company, Now);

        // Assert
        schedule.Days.Count.ShouldBe(7);
        schedule.For(DayOfWeek.Monday).Describe().ShouldBe("08:00 às 17:00, com 1h de intervalo");
        schedule.For(DayOfWeek.Saturday).IsWorkday.ShouldBeFalse();
        schedule.For(DayOfWeek.Sunday).IsWorkday.ShouldBeFalse();
    }

    [Fact]
    public void Define_WithAMissingDay_IsRejected()
    {
        // Arrange — a missing day is not a day off, and treating it as one
        // would turn a partial edit into an unpaid Saturday
        var sixDays = Enum.GetValues<DayOfWeek>()
            .Where(day => day != DayOfWeek.Friday)
            .Select(WorkDaySchedule.Off);

        // Act
        var result = CompanyWorkSchedule.Define(Company, sixDays, Now);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors[0].Code.ShouldBe("SCHEDULE_WEEK_INCOMPLETE");
    }

    [Fact]
    public void Define_WithADayListedTwice_IsRejected()
    {
        // Arrange
        var days = Enum.GetValues<DayOfWeek>().Select(WorkDaySchedule.Off).ToList();
        days[0] = WorkDaySchedule.Off(DayOfWeek.Monday);

        // Act
        var result = CompanyWorkSchedule.Define(Company, days, Now);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors[0].Code.ShouldBe("SCHEDULE_WEEK_INCOMPLETE");
    }

    [Fact]
    public void Redefine_ReplacesTheWeekAndStampsTheChange()
    {
        // Arrange
        var schedule = CompanyWorkSchedule.Default(Company, Now);
        var later = Now.AddDays(10);

        // Act — a shorter Friday
        var days = WorkWeek.MondayToFriday(new TimeOnly(9, 0), null, null, new TimeOnly(15, 0));
        var result = schedule.Redefine(days, later);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        schedule.For(DayOfWeek.Monday).Describe().ShouldBe("09:00 às 15:00");
        schedule.UpdatedAt.ShouldBe(later);
    }

    [Fact]
    public void EmployeeSchedule_AppliesOnlyWithinItsPeriod()
    {
        // Arrange
        var schedule = EmployeeWorkSchedule.Define(
            Company,
            Employee,
            WorkWeek.MondayToFriday(new TimeOnly(6, 0), null, null, new TimeOnly(14, 0)),
            new DateOnly(2026, 6, 1),
            Now).Value;

        // Act & Assert
        schedule.AppliesOn(new DateOnly(2026, 5, 31)).ShouldBeFalse();
        schedule.AppliesOn(new DateOnly(2026, 6, 1)).ShouldBeTrue();
        schedule.AppliesOn(new DateOnly(2027, 1, 1)).ShouldBeTrue("it is still current");
    }

    [Fact]
    public void CloseBefore_EndsThePeriodTheDayBeforeItsReplacement()
    {
        // Arrange — changing hours must not rewrite the past
        var schedule = EmployeeWorkSchedule.Define(
            Company, Employee, WorkWeek.MondayToFriday(new TimeOnly(6, 0), null, null, new TimeOnly(14, 0)),
            new DateOnly(2026, 6, 1), Now).Value;

        // Act
        var result = schedule.CloseBefore(new DateOnly(2026, 9, 1));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        schedule.EffectiveTo.ShouldBe(new DateOnly(2026, 8, 31));
        schedule.IsCurrent.ShouldBeFalse();
        schedule.AppliesOn(new DateOnly(2026, 8, 31)).ShouldBeTrue();
        schedule.AppliesOn(new DateOnly(2026, 9, 1)).ShouldBeFalse();
    }

    [Fact]
    public void CloseBefore_ItsOwnStart_IsRejected()
    {
        // Arrange — a period ending before it begins governs nothing, and the
        // day it was meant to cover would be left with no schedule at all
        var schedule = EmployeeWorkSchedule.Define(
            Company, Employee, WorkWeek.MondayToFriday(new TimeOnly(6, 0), null, null, new TimeOnly(14, 0)),
            new DateOnly(2026, 6, 1), Now).Value;

        // Act
        var result = schedule.CloseBefore(new DateOnly(2026, 5, 1));

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors[0].Code.ShouldBe("SCHEDULE_EFFECTIVE_FROM_IN_THE_PAST");
    }

    [Fact]
    public void EffectiveWorkDay_PrefersTheEmployeesOwnHours()
    {
        // Arrange
        var company = CompanyWorkSchedule.Default(Company, Now);
        var own = EmployeeWorkSchedule.Define(
            Company, Employee, WorkWeek.MondayToFriday(new TimeOnly(6, 0), null, null, new TimeOnly(14, 0)),
            new DateOnly(2026, 6, 1), Now).Value;

        // Act — a Monday inside the employee's period
        var day = EffectiveWorkDay.On(new DateOnly(2026, 8, 31), [own], company);

        // Assert
        day.Describe().ShouldBe("06:00 às 14:00");
    }

    [Fact]
    public void EffectiveWorkDay_FallsBackToTheCompany_OutsideThatPeriod()
    {
        // Arrange
        var company = CompanyWorkSchedule.Default(Company, Now);
        var own = EmployeeWorkSchedule.Define(
            Company, Employee, WorkWeek.MondayToFriday(new TimeOnly(6, 0), null, null, new TimeOnly(14, 0)),
            new DateOnly(2026, 6, 1), Now).Value;
        own.CloseBefore(new DateOnly(2026, 8, 1));

        // Act — a Monday after the employee's period closed
        var day = EffectiveWorkDay.On(new DateOnly(2026, 8, 31), [own], company);

        // Assert
        day.Describe().ShouldBe("08:00 às 17:00, com 1h de intervalo");
    }

    [Fact]
    public void EffectiveWorkDay_WithNoScheduleAtAll_IsADayOff()
    {
        // Arrange & Act — a company that has not set its hours has told us
        // nothing, and inventing a nine-to-five would produce lateness nobody
        // agreed to
        var day = EffectiveWorkDay.On(new DateOnly(2026, 8, 31), null, null);

        // Assert
        day.IsWorkday.ShouldBeFalse();
        day.ExpectedPunches.ShouldBe(0);
    }
}
