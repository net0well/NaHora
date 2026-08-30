using NaHora.Domain.Abstractions;
using NaHora.Domain.Errors;
using NaHora.Domain.ValueObjects;
using NaHora.Shared.Results;

namespace NaHora.Domain.Companies;

/// <summary>
/// Tenant root. Every business entity belongs to exactly one company.
/// </summary>
[Audit]
public sealed class Company : AggregateRoot
{
    public const string DefaultTimeZoneId = "America/Sao_Paulo";

    // EF Core materialises through this constructor.
    private Company()
    {
        LegalName = string.Empty;
        TimeZoneId = DefaultTimeZoneId;
    }

    private Company(
        Cnpj cnpj,
        string legalName,
        string? tradeName,
        EmailAddress email,
        string timeZoneId,
        DateTimeOffset createdAt)
    {
        Cnpj = cnpj;
        LegalName = legalName;
        TradeName = tradeName;
        Email = email;
        TimeZoneId = timeZoneId;
        Status = CompanyStatus.PendingEmailConfirmation;
        CreatedAt = createdAt;
    }

    public Cnpj Cnpj { get; private set; }

    public string LegalName { get; private set; }

    public string? TradeName { get; private set; }

    public EmailAddress Email { get; private set; }

    /// <summary>
    /// IANA identifier. Brazil spans four time zones, so comparing a punch
    /// against a shift schedule needs the company's own zone, not the server's.
    /// </summary>
    public string TimeZoneId { get; private set; }

    public CompanyStatus Status { get; private set; }

    /// <summary>
    /// False when the tax registry lookup could not confirm the CNPJ. An outage
    /// there degrades the record; it does not block the sign-up.
    /// </summary>
    public bool RegistryVerified { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? ActivatedAt { get; private set; }

    /// <summary>Whether the company may authenticate users and accept punches.</summary>
    public bool IsOperational => Status == CompanyStatus.Active;

    /// <summary>
    /// Creates a company pending email confirmation.
    /// </summary>
    /// <remarks>
    /// Collects every problem before returning, so the sign-up form can show
    /// all of them at once instead of one per round trip.
    /// </remarks>
    public static Result<Company> Register(
        Cnpj cnpj,
        string legalName,
        string? tradeName,
        EmailAddress email,
        string timeZoneId,
        DateTimeOffset createdAt)
    {
        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(legalName))
        {
            errors.Add(CompanyErrors.LegalNameRequired());
        }

        if (!IsKnownTimeZone(timeZoneId))
        {
            errors.Add(CompanyErrors.TimeZoneInvalid(timeZoneId));
        }

        if (errors.Count > 0)
        {
            return Result.Failure<Company>(errors);
        }

        return new Company(cnpj, legalName.Trim(), tradeName?.Trim(), email, timeZoneId, createdAt);
    }

    public Result ConfirmEmail(DateTimeOffset confirmedAt)
    {
        if (Status != CompanyStatus.PendingEmailConfirmation)
        {
            return Result.Failure(CompanyErrors.AlreadyConfirmed());
        }

        Status = CompanyStatus.Active;
        ActivatedAt = confirmedAt;

        return Result.Success();
    }

    public void MarkRegistryVerified() => RegistryVerified = true;

    public Result Suspend()
    {
        if (Status != CompanyStatus.Active)
        {
            return Result.Failure(CompanyErrors.CannotSuspend(Status));
        }

        Status = CompanyStatus.Suspended;

        return Result.Success();
    }

    public Result Reactivate()
    {
        if (Status != CompanyStatus.Suspended)
        {
            return Result.Failure(CompanyErrors.CannotReactivate(Status));
        }

        Status = CompanyStatus.Active;

        return Result.Success();
    }

    /// <summary>
    /// Terminal. Historical records stay intact and exportable: attendance data
    /// carries a statutory retention period that outlives the commercial
    /// relationship.
    /// </summary>
    public Result Deactivate()
    {
        if (Status == CompanyStatus.Inactive)
        {
            return Result.Failure(CompanyErrors.AlreadyInactive());
        }

        Status = CompanyStatus.Inactive;

        return Result.Success();
    }

    public Result UpdateDetails(string legalName, string? tradeName, EmailAddress email, string timeZoneId)
    {
        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(legalName))
        {
            errors.Add(CompanyErrors.LegalNameRequired());
        }

        if (!IsKnownTimeZone(timeZoneId))
        {
            errors.Add(CompanyErrors.TimeZoneInvalid(timeZoneId));
        }

        if (errors.Count > 0)
        {
            return Result.Failure(errors);
        }

        LegalName = legalName.Trim();
        TradeName = tradeName?.Trim();
        Email = email;

        // Changing the zone does not reinterpret past punches: each record
        // freezes the offset applied when it was taken.
        TimeZoneId = timeZoneId;

        return Result.Success();
    }

    private static bool IsKnownTimeZone(string timeZoneId) =>
        !string.IsNullOrWhiteSpace(timeZoneId) && TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out _);
}
