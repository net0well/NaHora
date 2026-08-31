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
        Address = null!;
    }

    private Company(
        Cnpj cnpj,
        string legalName,
        string? tradeName,
        EmailAddress email,
        Address address,
        string timeZoneId,
        DateTimeOffset createdAt,
        PhoneNumber? phone,
        string? stateRegistration,
        string? municipalRegistration,
        DateOnly? foundedOn)
    {
        Cnpj = cnpj;
        LegalName = legalName;
        TradeName = tradeName;
        Email = email;
        Address = address;
        TimeZoneId = timeZoneId;
        Status = CompanyStatus.PendingEmailConfirmation;
        CreatedAt = createdAt;
        Phone = phone;
        StateRegistration = stateRegistration;
        MunicipalRegistration = municipalRegistration;
        FoundedOn = foundedOn;
    }

    public Cnpj Cnpj { get; private set; }

    public string LegalName { get; private set; }

    public string? TradeName { get; private set; }

    public EmailAddress Email { get; private set; }

    /// <summary>
    /// Where the establishment is registered.
    /// </summary>
    /// <remarks>
    /// Not a punch location and never compared against one. An employee working
    /// externally punches far from here by design, and treating this as a fence
    /// would be exactly the restriction Art. 78 forbids.
    /// </remarks>
    public Address Address { get; private set; }

    public PhoneNumber? Phone { get; private set; }

    /// <summary>
    /// Inscrição estadual, or null when the company is exempt.
    /// </summary>
    /// <remarks>
    /// Free text rather than a validated value object: the format differs by
    /// state, and "ISENTO" is a legitimate entry in several of them.
    /// </remarks>
    public string? StateRegistration { get; private set; }

    /// <summary>Inscrição municipal, or null when there is none.</summary>
    public string? MunicipalRegistration { get; private set; }

    public DateOnly? FoundedOn { get; private set; }

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
        Address address,
        string timeZoneId,
        DateTimeOffset createdAt,
        PhoneNumber? phone = null,
        string? stateRegistration = null,
        string? municipalRegistration = null,
        DateOnly? foundedOn = null)
    {
        ArgumentNullException.ThrowIfNull(address);

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

        return new Company(
            cnpj,
            legalName.Trim(),
            tradeName?.Trim(),
            email,
            address,
            timeZoneId,
            createdAt,
            phone,
            NullIfBlank(stateRegistration),
            NullIfBlank(municipalRegistration),
            foundedOn);
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

    public Result UpdateDetails(
        string legalName,
        string? tradeName,
        EmailAddress email,
        Address address,
        string timeZoneId,
        PhoneNumber? phone = null,
        string? stateRegistration = null,
        string? municipalRegistration = null,
        DateOnly? foundedOn = null)
    {
        ArgumentNullException.ThrowIfNull(address);

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
        Address = address;
        Phone = phone;
        StateRegistration = NullIfBlank(stateRegistration);
        MunicipalRegistration = NullIfBlank(municipalRegistration);
        FoundedOn = foundedOn;

        // Changing the zone does not reinterpret past punches: each record
        // freezes the offset applied when it was taken.
        TimeZoneId = timeZoneId;

        return Result.Success();
    }

    /// <summary>
    /// The company's UTC offset at a given instant.
    /// </summary>
    /// <remarks>
    /// Resolved for that moment rather than for now, so a punch made before a
    /// daylight-saving change keeps the offset that was in force when it
    /// happened.
    ///
    /// Falls back to UTC instead of throwing when the identifier is unknown. A
    /// misconfigured timezone must not stop anyone punching or reading a clock,
    /// for the same reason nothing else here refuses a record.
    /// </remarks>
    public static TimeSpan OffsetAt(string timeZoneId, DateTimeOffset instant) =>
        TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out var zone)
            ? zone.GetUtcOffset(instant)
            : TimeSpan.Zero;

    /// <summary>The calendar day that instant falls on, in company time.</summary>
    public static DateOnly LocalDateAt(string timeZoneId, DateTimeOffset instant) =>
        DateOnly.FromDateTime((instant + OffsetAt(timeZoneId, instant)).DateTime);

    /// <summary>
    /// The UTC interval covering one local day, start inclusive, end exclusive.
    /// </summary>
    /// <remarks>
    /// What a query needs to ask for "everything that happened on the 30th"
    /// against a column stored in UTC.
    ///
    /// The offset is sampled at local noon of each day rather than at midnight.
    /// Midnight is exactly where a daylight-saving change can land — Brazil's
    /// own former rule moved the clock at midnight — and sampling inside the
    /// gap gives an offset for a wall time that never existed. Noon is never in
    /// one.
    /// </remarks>
    public static (DateTimeOffset StartUtc, DateTimeOffset EndUtc) LocalDayWindow(
        string timeZoneId,
        DateOnly localDate)
    {
        var nextDate = localDate.AddDays(1);

        var startOffset = OffsetAtLocalNoon(timeZoneId, localDate);
        var endOffset = OffsetAtLocalNoon(timeZoneId, nextDate);

        return (
            new DateTimeOffset(localDate.ToDateTime(TimeOnly.MinValue), startOffset).ToUniversalTime(),
            new DateTimeOffset(nextDate.ToDateTime(TimeOnly.MinValue), endOffset).ToUniversalTime());
    }

    private static TimeSpan OffsetAtLocalNoon(string timeZoneId, DateOnly localDate)
    {
        if (!TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out var zone))
        {
            return TimeSpan.Zero;
        }

        return zone.GetUtcOffset(
            DateTime.SpecifyKind(localDate.ToDateTime(new TimeOnly(12, 0)), DateTimeKind.Unspecified));
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool IsKnownTimeZone(string timeZoneId) =>
        !string.IsNullOrWhiteSpace(timeZoneId) && TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out _);
}
