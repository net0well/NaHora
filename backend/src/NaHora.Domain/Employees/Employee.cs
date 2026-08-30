using NaHora.Domain.Abstractions;
using NaHora.Domain.Errors;
using NaHora.Domain.ValueObjects;
using NaHora.Shared.Results;

namespace NaHora.Domain.Employees;

/// <summary>
/// The employment relationship. Source of the regulatory export and of the
/// working-hours calculation.
/// </summary>
[Audit]
public sealed class Employee : AggregateRoot, ITenantOwned
{
    private Employee()
    {
        FullName = string.Empty;
        Registration = string.Empty;
    }

    private Employee(
        Guid companyId,
        Guid userId,
        Cpf cpf,
        Pis? pis,
        string fullName,
        string registration,
        string? position,
        DateOnly hiredAt,
        DateTimeOffset createdAt)
    {
        CompanyId = companyId;
        UserId = userId;
        Cpf = cpf;
        Pis = pis;
        FullName = fullName;
        Registration = registration;
        Position = position;
        HiredAt = hiredAt;
        Status = EmployeeStatus.Active;
        CreatedAt = createdAt;
    }

    public Guid CompanyId { get; private set; }

    public Guid UserId { get; private set; }

    /// <summary>Immutable. Already emitted in exported AFD files.</summary>
    public Cpf Cpf { get; private set; }

    /// <summary>
    /// Optional on purpose. The AFD requires it, but a missing PIS must surface
    /// as a warning on export rather than block a hire or a punch.
    /// </summary>
    public Pis? Pis { get; private set; }

    public string FullName { get; private set; }

    public string Registration { get; private set; }

    public string? Position { get; private set; }

    public DateOnly HiredAt { get; private set; }

    public DateOnly? TerminatedAt { get; private set; }

    public EmployeeStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>An employee without a PIS cannot appear in the AFD export.</summary>
    public bool IsExportableToAfd => Pis is not null;

    public static Result<Employee> Hire(
        Guid companyId,
        Guid userId,
        Cpf cpf,
        Pis? pis,
        string fullName,
        string registration,
        string? position,
        DateOnly hiredAt,
        DateTimeOffset createdAt)
    {
        var errors = new List<Error>();

        if (companyId == Guid.Empty)
        {
            errors.Add(EmployeeErrors.CompanyRequired());
        }

        if (userId == Guid.Empty)
        {
            errors.Add(EmployeeErrors.UserRequired());
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            errors.Add(EmployeeErrors.NameRequired());
        }

        if (string.IsNullOrWhiteSpace(registration))
        {
            errors.Add(EmployeeErrors.RegistrationRequired());
        }

        if (errors.Count > 0)
        {
            return Result.Failure<Employee>(errors);
        }

        return new Employee(
            companyId,
            userId,
            cpf,
            pis,
            fullName.Trim(),
            registration.Trim(),
            position?.Trim(),
            hiredAt,
            createdAt);
    }

    public Result PutOnLeave()
    {
        if (Status != EmployeeStatus.Active)
        {
            return Result.Failure(EmployeeErrors.NotActive());
        }

        Status = EmployeeStatus.OnLeave;

        return Result.Success();
    }

    public Result ReturnFromLeave()
    {
        if (Status != EmployeeStatus.OnLeave)
        {
            return Result.Failure(EmployeeErrors.NotOnLeave());
        }

        Status = EmployeeStatus.Active;

        return Result.Success();
    }

    /// <summary>
    /// Terminal. Rehiring creates a new employment record: the periods must stay
    /// separate in the regulatory export.
    /// </summary>
    public Result Terminate(DateOnly terminatedAt)
    {
        var errors = new List<Error>();

        if (Status == EmployeeStatus.Terminated)
        {
            errors.Add(EmployeeErrors.AlreadyTerminated());
        }

        if (terminatedAt < HiredAt)
        {
            errors.Add(EmployeeErrors.TerminationDateBeforeHire());
        }

        if (errors.Count > 0)
        {
            return Result.Failure(errors);
        }

        Status = EmployeeStatus.Terminated;
        TerminatedAt = terminatedAt;

        return Result.Success();
    }

    public void AssignPis(Pis pis) => Pis = pis;

    public Result UpdateDetails(string fullName, string registration, string? position)
    {
        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(fullName))
        {
            errors.Add(EmployeeErrors.NameRequired());
        }

        if (string.IsNullOrWhiteSpace(registration))
        {
            errors.Add(EmployeeErrors.RegistrationRequired());
        }

        if (errors.Count > 0)
        {
            return Result.Failure(errors);
        }

        FullName = fullName.Trim();
        Registration = registration.Trim();
        Position = position?.Trim();

        return Result.Success();
    }

    /// <summary>
    /// Whether this employee may still record attendance on a given date.
    /// </summary>
    /// <remarks>
    /// Deliberately permissive. Ordinance 671/2021, article 78 §1, forbids a
    /// time clock from restricting a punch, so being on leave never blocks the
    /// record — it flags it for review instead. Only a date after termination
    /// falls outside the employment relationship entirely.
    /// </remarks>
    public bool CanRecordAttendanceOn(DateOnly date) =>
        TerminatedAt is not { } terminated || date <= terminated;

    /// <summary>Whether a punch on this date warrants a security event.</summary>
    public bool AttendanceRequiresReviewOn(DateOnly date) =>
        Status == EmployeeStatus.OnLeave || (TerminatedAt is { } terminated && date > terminated);
}
