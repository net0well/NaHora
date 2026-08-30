using NaHora.Domain.Adjustments;
using NaHora.Domain.Attendance;

namespace NaHora.UnitTests.Adjustments;

/// <summary>
/// Correcting a time record: who may ask, who may agree, and what stays
/// untouched.
/// </summary>
public sealed class PunchAdjustmentTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 30, 17, 0, 0, TimeSpan.Zero);
    private static readonly Guid Company = Guid.CreateVersion7();
    private static readonly Guid Employee = Guid.CreateVersion7();
    private static readonly Guid EmployeeUser = Guid.CreateVersion7();
    private static readonly Guid Manager = Guid.CreateVersion7();
    private static readonly Guid ManagerEmployee = Guid.CreateVersion7();

    private static AdjustmentJustification Forgot =>
        AdjustmentJustificationCatalogue.All.Single(
            j => j.Code == AdjustmentJustificationCatalogue.ForgotToPunch);

    private static AdjustmentJustification Other =>
        AdjustmentJustificationCatalogue.All.Single(
            j => j.Code == AdjustmentJustificationCatalogue.Other);

    [Fact]
    public void RequestTimeChange_StartsPendingAndKeepsTheOriginalTime()
    {
        // Arrange
        var punch = NewPunch(Now.AddHours(-9));

        // Act
        var result = PunchAdjustment.RequestTimeChange(
            Company, Employee, punch, Now.AddHours(-9).AddMinutes(-15),
            Forgot, notes: null, EmployeeUser, Now);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Status.ShouldBe(PunchAdjustmentStatus.Pending);
        result.Value.Counts.ShouldBeFalse("nothing counts until a manager agrees");
        result.Value.OriginalOccurredAt.ShouldBe(punch.OccurredAt);
        result.Value.PunchId.ShouldBe(punch.Id);

        // The punch itself is untouched — it is the row that goes signed into
        // the AFD
        punch.OccurredAt.ShouldBe(Now.AddHours(-9));
    }

    [Fact]
    public void RequestInclusion_CarriesNoOriginal()
    {
        // Arrange & Act — nothing was recorded, so there is nothing to quote
        var result = PunchAdjustment.RequestInclusion(
            Company, Employee, Now.AddHours(-2), PunchDirection.Exit,
            Forgot, notes: null, EmployeeUser, Now);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Kind.ShouldBe(PunchAdjustmentKind.Inclusion);
        result.Value.PunchId.ShouldBeNull();
        result.Value.OriginalOccurredAt.ShouldBeNull();
    }

    [Fact]
    public void RequestInclusion_ForATimeThatHasNotHappened_IsRefused()
    {
        // Arrange & Act — a time nobody has reached is a schedule, not a record
        var result = PunchAdjustment.RequestInclusion(
            Company, Employee, Now.AddHours(3), PunchDirection.Exit,
            Forgot, notes: null, EmployeeUser, Now);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors[0].Code.ShouldBe("ADJUSTMENT_OCCURRED_AT_IN_THE_FUTURE");
    }

    [Fact]
    public void Request_WithoutAJustification_IsRefused()
    {
        // Arrange & Act
        var result = PunchAdjustment.RequestInclusion(
            Company, Employee, Now.AddHours(-1), PunchDirection.Exit,
            justification: null, notes: null, EmployeeUser, Now);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldContain(error => error.Code == "JUSTIFICATION_REQUIRED");
    }

    [Fact]
    public void Request_WithTheCatchAllAndNoNotes_IsRefused()
    {
        // Arrange & Act — left free, "Outro" becomes the way to avoid choosing
        // and the catalogue stops distinguishing anything
        var result = PunchAdjustment.RequestInclusion(
            Company, Employee, Now.AddHours(-1), PunchDirection.Exit,
            Other, notes: "   ", EmployeeUser, Now);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldContain(error => error.Code == "JUSTIFICATION_NOTES_REQUIRED");
    }

    [Fact]
    public void Approve_ByAManagerOnSomebodyElsesRecord_MakesTheAdjustedTimeCount()
    {
        // Arrange
        var adjustment = PendingInclusion();

        // Act
        var result = adjustment.Approve(Manager, ManagerEmployee, "Confere com o registro do portão.", Now);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        adjustment.Status.ShouldBe(PunchAdjustmentStatus.Approved);
        adjustment.Counts.ShouldBeTrue();
        adjustment.DecidedByUserId.ShouldBe(Manager);
        adjustment.DecidedAt.ShouldBe(Now);
    }

    [Fact]
    public void Approve_OnYourOwnRecord_IsRefused()
    {
        // Arrange — approving a correction to your own timesheet is editing
        // your own timesheet with extra steps
        var adjustment = PendingInclusion();

        // Act — the decider is the employee the record belongs to
        var result = adjustment.Approve(Manager, Employee, decisionNotes: null, Now);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors[0].Code.ShouldBe("ADJUSTMENT_CANNOT_DECIDE_OWN_RECORD");
        adjustment.Status.ShouldBe(PunchAdjustmentStatus.Pending);
    }

    [Fact]
    public void Approve_ByAnAdministratorWithNoEmploymentRecord_IsAllowed()
    {
        // Arrange — an administrator who never punches cannot be correcting
        // their own record
        var adjustment = PendingInclusion();

        // Act
        var result = adjustment.Approve(Manager, managerEmployeeId: null, decisionNotes: null, Now);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Reject_KeepsTheRequestAsPartOfTheTrail()
    {
        // Arrange
        var adjustment = PendingInclusion();

        // Act
        var result = adjustment.Reject(Manager, ManagerEmployee, "Não bate com a portaria.", Now);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        adjustment.Status.ShouldBe(PunchAdjustmentStatus.Rejected);
        adjustment.Counts.ShouldBeFalse();
        adjustment.DecisionNotes.ShouldBe("Não bate com a portaria.");
    }

    [Fact]
    public void Deciding_Twice_IsRefused()
    {
        // Arrange
        var adjustment = PendingInclusion();
        adjustment.Approve(Manager, ManagerEmployee, null, Now);

        // Act
        var result = adjustment.Reject(Manager, ManagerEmployee, null, Now.AddMinutes(1));

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors[0].Code.ShouldBe("ADJUSTMENT_ALREADY_DECIDED");
        adjustment.Status.ShouldBe(PunchAdjustmentStatus.Approved);
    }

    [Fact]
    public void Attach_AfterADecision_IsRefused()
    {
        // Arrange — evidence added after the fact would change what the
        // decision was made on
        var adjustment = PendingInclusion();
        adjustment.Approve(Manager, ManagerEmployee, null, Now);

        // Act
        var result = adjustment.Attach("atestado.jpg");

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors[0].Code.ShouldBe("ADJUSTMENT_ALREADY_DECIDED");
    }

    [Fact]
    public void Attach_WhilePending_RecordsTheObjectKey()
    {
        // Arrange
        var adjustment = PendingInclusion();

        // Act
        var result = adjustment.Attach("  company/atestado.jpg  ");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        adjustment.AttachmentKey.ShouldBe("company/atestado.jpg");
    }

    private static PunchAdjustment PendingInclusion() =>
        PunchAdjustment.RequestInclusion(
            Company, Employee, Now.AddHours(-1), PunchDirection.Exit,
            Forgot, notes: null, EmployeeUser, Now).Value;

    private static Punch NewPunch(DateTimeOffset occurredAt) =>
        Punch.Register(
            Company,
            Employee,
            "client-1",
            nsr: 1,
            PunchDirection.Entry,
            PunchOrigin.Online,
            occurredAt,
            occurredAt,
            TimeSpan.FromHours(-3),
            GeoLocation.Create(-23.5505, -46.6333, 12, occurredAt).Value,
            DeviceIntegrity.Create("device", false, occurredAt, occurredAt)).Value;
}
