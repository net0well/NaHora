using NaHora.Domain.Employees;
using NaHora.Domain.ValueObjects;

namespace NaHora.UnitTests.Domain;

public sealed class EmployeeTests
{
    private static readonly Guid CompanyId = Guid.CreateVersion7();
    private static readonly Guid UserId = Guid.CreateVersion7();
    private static readonly Cpf ValidCpf = Cpf.Create("529.982.247-25").Value;
    private static readonly DateOnly HireDate = new(2026, 1, 15);
    private static readonly DateTimeOffset Now = new(2026, 8, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Hire_WhenDataIsValid_Succeeds()
    {
        // Act
        var result = Hire();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Status.ShouldBe(EmployeeStatus.Active);
        result.Value.CompanyId.ShouldBe(CompanyId);
    }

    [Fact]
    public void Hire_WhenSeveralFieldsAreMissing_ReportsAllOfThemAtOnce()
    {
        // Act — the notification pattern earning its keep: one round trip, all
        // the problems.
        var result = Employee.Hire(
            companyId: Guid.Empty,
            userId: Guid.Empty,
            cpf: ValidCpf,
            pis: null,
            fullName: "   ",
            registration: "",
            position: null,
            hiredAt: HireDate,
            createdAt: Now);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Select(error => error.Code).ShouldBe(
        [
            "EMPLOYEE_COMPANY_REQUIRED",
            "EMPLOYEE_USER_REQUIRED",
            "EMPLOYEE_NAME_REQUIRED",
            "EMPLOYEE_REGISTRATION_REQUIRED"
        ]);
    }

    [Fact]
    public void Hire_WithoutPis_SucceedsButIsNotExportable()
    {
        // Act — a missing PIS must never block a hire; it surfaces as a warning
        // when the AFD is generated.
        var result = Hire(pis: null);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.IsExportableToAfd.ShouldBeFalse();
    }

    [Fact]
    public void PutOnLeave_WhenAlreadyOnLeave_Fails()
    {
        // Arrange
        var employee = Hire().Value;
        employee.PutOnLeave();

        // Act
        var result = employee.PutOnLeave();

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors[0].Code.ShouldBe("EMPLOYEE_NOT_ACTIVE");
    }

    [Fact]
    public void Terminate_WhenDatePrecedesHire_Fails()
    {
        // Arrange
        var employee = Hire().Value;

        // Act
        var result = employee.Terminate(HireDate.AddDays(-1));

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors[0].Code.ShouldBe("TERMINATION_DATE_BEFORE_HIRE");
        employee.Status.ShouldBe(EmployeeStatus.Active);
    }

    [Fact]
    public void CanRecordAttendanceOn_WhenOnLeave_ReturnsTrue()
    {
        // Arrange
        var employee = Hire().Value;
        employee.PutOnLeave();

        // Act & Assert — Ordinance 671/2021, article 78 §1, forbids the clock
        // from restricting a punch. Being on leave flags it, never blocks it.
        employee.CanRecordAttendanceOn(new DateOnly(2026, 8, 29)).ShouldBeTrue();
        employee.AttendanceRequiresReviewOn(new DateOnly(2026, 8, 29)).ShouldBeTrue();
    }

    [Fact]
    public void CanRecordAttendanceOn_WhenDateIsOnOrBeforeTermination_ReturnsTrue()
    {
        // Arrange
        var employee = Hire().Value;
        var lastDay = new DateOnly(2026, 8, 20);
        employee.Terminate(lastDay);

        // Act & Assert — a late punch for a day actually worked stays valid
        employee.CanRecordAttendanceOn(lastDay).ShouldBeTrue();
        employee.CanRecordAttendanceOn(lastDay.AddDays(-1)).ShouldBeTrue();
    }

    [Fact]
    public void CanRecordAttendanceOn_WhenDateIsAfterTermination_ReturnsFalse()
    {
        // Arrange
        var employee = Hire().Value;
        var lastDay = new DateOnly(2026, 8, 20);
        employee.Terminate(lastDay);

        // Act & Assert — outside the employment relationship entirely
        employee.CanRecordAttendanceOn(lastDay.AddDays(1)).ShouldBeFalse();
    }

    private static Shared.Results.Result<Employee> Hire(Pis? pis = null) =>
        Employee.Hire(
            companyId: CompanyId,
            userId: UserId,
            cpf: ValidCpf,
            pis: pis,
            fullName: "Maria Souza",
            registration: "00123",
            position: "Analista",
            hiredAt: HireDate,
            createdAt: Now);
}
