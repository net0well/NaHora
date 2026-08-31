using NaHora.Domain.Adjustments;
using NaHora.Domain.Attendance;

namespace NaHora.UnitTests.Attendance;

/// <summary>
/// Reading the record as it counts.
/// </summary>
/// <remarks>
/// The punches table never changes — the row goes signed into the AFD. What
/// changes is the reading, and these are the rules that decide it.
/// </remarks>
public sealed class EffectivePunchRecordTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 30, 20, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan SaoPaulo = TimeSpan.FromHours(-3);
    private static readonly Guid Company = Guid.CreateVersion7();
    private static readonly Guid Employee = Guid.CreateVersion7();
    private static readonly Guid EmployeeUser = Guid.CreateVersion7();
    private static readonly Guid Manager = Guid.CreateVersion7();

    private static AdjustmentJustification Forgot =>
        AdjustmentJustificationCatalogue.All.Single(
            justification => justification.Code == AdjustmentJustificationCatalogue.ForgotToPunch);

    [Fact]
    public void Merge_WithNoTreatments_ReadsTheRecordExactlyAsCaptured()
    {
        // Arrange
        var punch = NewPunch(Now, nsr: 1);

        // Act
        var record = EffectivePunchRecord.Merge([punch], [], _ => SaoPaulo);

        // Assert
        var entry = record.ShouldHaveSingleItem();
        entry.Source.ShouldBe(EffectivePunchSource.Recorded);
        entry.OccurredAt.ShouldBe(punch.OccurredAt);
        entry.OriginalOccurredAt.ShouldBeNull();
        entry.Nsr.ShouldBe(1);
    }

    [Fact]
    public void Merge_WithAPendingRequest_ChangesNothing()
    {
        // Arrange — asking is not the same as being granted, and a screen that
        // showed the asked-for time would let anyone move their own hours by
        // requesting
        var punch = NewPunch(Now, nsr: 1);
        var pending = TimeChange(punch, Now.AddMinutes(-30));

        // Act
        var record = EffectivePunchRecord.Merge([punch], [pending], _ => SaoPaulo);

        // Assert
        var entry = record.ShouldHaveSingleItem();
        entry.Source.ShouldBe(EffectivePunchSource.Recorded);
        entry.OccurredAt.ShouldBe(punch.OccurredAt);
    }

    [Fact]
    public void Merge_WithARefusedRequest_ChangesNothing()
    {
        // Arrange
        var punch = NewPunch(Now, nsr: 1);
        var refused = TimeChange(punch, Now.AddMinutes(-30));
        refused.Reject(Manager, managerEmployeeId: null, "Sem comprovante.", Now);

        // Act
        var record = EffectivePunchRecord.Merge([punch], [refused], _ => SaoPaulo);

        // Assert
        record.ShouldHaveSingleItem().OccurredAt.ShouldBe(punch.OccurredAt);
    }

    [Fact]
    public void Merge_WithAnApprovedCorrection_ShowsTheNewTimeBesideTheOldOne()
    {
        // Arrange — spec 014: a correction that hides what it replaced is an
        // edit, and the employee has the right to see their record was changed
        var punch = NewPunch(Now, nsr: 7);
        var approved = TimeChange(punch, Now.AddMinutes(-30));
        approved.Approve(Manager, managerEmployeeId: null, decisionNotes: null, Now);

        // Act
        var record = EffectivePunchRecord.Merge([punch], [approved], _ => SaoPaulo);

        // Assert
        var entry = record.ShouldHaveSingleItem();
        entry.Source.ShouldBe(EffectivePunchSource.Adjusted);
        entry.OccurredAt.ShouldBe(Now.AddMinutes(-30));
        entry.OriginalOccurredAt.ShouldBe(punch.OccurredAt);

        // The capture still happened; only the moment it counts at moved.
        entry.Nsr.ShouldBe(7);
        entry.Trust.ShouldBe(punch.Trust);
        entry.AccuracyMeters.ShouldBe(punch.Location.AccuracyMeters);
    }

    [Fact]
    public void Merge_WithTwoApprovedCorrections_LetsTheLatestDecisionGovern()
    {
        // Arrange — nothing stops an employee asking twice and a manager
        // agreeing twice; the later decision is the one taken with the earlier
        // one already in view
        var punch = NewPunch(Now, nsr: 1);

        var first = TimeChange(punch, Now.AddMinutes(-30));
        first.Approve(Manager, managerEmployeeId: null, decisionNotes: null, Now);

        var second = TimeChange(punch, Now.AddMinutes(-45));
        second.Approve(Manager, managerEmployeeId: null, decisionNotes: null, Now.AddMinutes(5));

        // Act
        var record = EffectivePunchRecord.Merge([punch], [first, second], _ => SaoPaulo);

        // Assert
        record.ShouldHaveSingleItem().OccurredAt.ShouldBe(Now.AddMinutes(-45));
    }

    [Fact]
    public void Merge_WithAnApprovedInclusion_AddsAMarkWithNoNsr()
    {
        // Arrange — no registrar ever numbered it, and sending a zero would put
        // a number an inspection could cite on a row that does not have one
        var inclusion = Inclusion(Now.AddHours(-9), PunchDirection.Entry);
        inclusion.Approve(Manager, managerEmployeeId: null, decisionNotes: null, Now);

        // Act
        var record = EffectivePunchRecord.Merge([], [inclusion], _ => SaoPaulo);

        // Assert
        var entry = record.ShouldHaveSingleItem();
        entry.Source.ShouldBe(EffectivePunchSource.Included);
        entry.Nsr.ShouldBeNull();
        entry.Trust.ShouldBeNull();
        entry.Origin.ShouldBeNull();
        entry.AccuracyMeters.ShouldBeNull();
        entry.OccurredAt.ShouldBe(Now.AddHours(-9));
    }

    [Fact]
    public void Merge_WithAnInclusion_FilesItUnderTheCompanysWorkingDay()
    {
        // Arrange — 02:00 UTC is still the previous evening in São Paulo, and
        // filing it under the UTC date would put the mark on a day the employee
        // did not work
        var inclusion = Inclusion(new DateTimeOffset(2026, 8, 31, 2, 0, 0, TimeSpan.Zero));
        inclusion.Approve(Manager, managerEmployeeId: null, decisionNotes: null, Now.AddDays(1));

        // Act
        var record = EffectivePunchRecord.Merge([], [inclusion], _ => SaoPaulo);

        // Assert
        record.ShouldHaveSingleItem().LocalDate.ShouldBe(new DateOnly(2026, 8, 30));
    }

    [Fact]
    public void Merge_WithBothKinds_OrdersByTheTimeThatCounts()
    {
        // Arrange — a corrected mark takes its new place in the day, and an
        // inclusion slots in where it says it happened
        var morning = NewPunch(Now.AddHours(-9), nsr: 1);
        var evening = NewPunch(Now, nsr: 2);

        var moved = TimeChange(evening, Now.AddHours(-10));
        moved.Approve(Manager, managerEmployeeId: null, decisionNotes: null, Now);

        var inclusion = Inclusion(Now.AddHours(-5));
        inclusion.Approve(Manager, managerEmployeeId: null, decisionNotes: null, Now);

        // Act
        var record = EffectivePunchRecord.Merge(
            [morning, evening], [moved, inclusion], _ => SaoPaulo);

        // Assert
        record.Select(entry => entry.OccurredAt).ShouldBe(
            [Now.AddHours(-10), Now.AddHours(-9), Now.AddHours(-5)]);
    }

    [Fact]
    public void Merge_WithAnInclusion_AsksForTheOffsetOfTheDayItLandsOn()
    {
        // Arrange — an inclusion granted for a day months back must render at
        // that day's offset, not at today's
        var asked = new List<DateTimeOffset>();
        var inclusion = Inclusion(Now.AddDays(-120));
        inclusion.Approve(Manager, managerEmployeeId: null, decisionNotes: null, Now);

        // Act
        EffectivePunchRecord.Merge([], [inclusion], instant =>
        {
            asked.Add(instant);
            return SaoPaulo;
        });

        // Assert
        asked.ShouldHaveSingleItem().ShouldBe(Now.AddDays(-120));
    }

    private static PunchAdjustment TimeChange(Punch punch, DateTimeOffset adjustedTo) =>
        PunchAdjustment.RequestTimeChange(
            Company, Employee, punch, adjustedTo, Forgot, notes: null, EmployeeUser, Now).Value;

    private static PunchAdjustment Inclusion(
        DateTimeOffset occurredAt,
        PunchDirection direction = PunchDirection.Entry) =>
        PunchAdjustment.RequestInclusion(
            Company, Employee, occurredAt, direction, Forgot, notes: null, EmployeeUser,
            occurredAt.AddHours(1)).Value;

    private static Punch NewPunch(DateTimeOffset occurredAt, long nsr) =>
        Punch.Register(
            Company,
            Employee,
            $"client-{nsr}",
            nsr,
            PunchDirection.Entry,
            PunchOrigin.Online,
            occurredAt,
            occurredAt,
            SaoPaulo,
            GeoLocation.Create(-23.5505, -46.6333, 12, occurredAt).Value,
            DeviceIntegrity.Create("device", false, occurredAt, occurredAt)).Value;
}
