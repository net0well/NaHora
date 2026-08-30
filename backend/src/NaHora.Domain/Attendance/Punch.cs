using NaHora.Domain.Abstractions;
using NaHora.Domain.Errors;
using NaHora.Shared.Results;

namespace NaHora.Domain.Attendance;

/// <summary>
/// One time-clock record. The legal artefact the whole product exists to
/// produce.
/// </summary>
/// <remarks>
/// <para>
/// <b>Immutable once created.</b> No method here changes a recorded punch, and
/// that absence is the design: Portaria 671 puts the original record in the
/// signed AFD, so rewriting it would invalidate the chain rather than fix a
/// line. Corrections are a separate aggregate that points at this one.
/// </para>
/// <para>
/// <b>Nothing refuses a punch.</b> Mocked location, useless accuracy, a device
/// clock hours out of step: all of it is classified into <see cref="Trust"/>
/// and recorded. Art. 78 forbids the registrar from restricting the record, and
/// that does not make an exception for suspicion.
/// </para>
/// </remarks>
[Audit]
public sealed class Punch : AggregateRoot, ITenantOwned
{
    /// <summary>
    /// Accuracy beyond which the fix stops being useful evidence.
    /// </summary>
    /// <remarks>
    /// A hundred metres covers a city block, which cannot tell the office from
    /// the cafe next door. Below it the reading narrows things usefully; above
    /// it the punch still counts, it just proves less.
    /// </remarks>
    public const double DegradedAccuracyMeters = 100;

    /// <summary>
    /// Clock difference beyond which the device disagrees enough to look
    /// deliberate.
    /// </summary>
    /// <remarks>
    /// Five minutes absorbs an unsynchronised phone. Someone shifting their
    /// clock to punch early moves it by far more, because a smaller change
    /// would not be worth doing.
    /// </remarks>
    public const int SuspiciousClockSkewSeconds = 300;

    private Punch()
    {
        ClientId = string.Empty;
        Location = null!;
        Integrity = null!;
    }

    private Punch(
        Guid companyId,
        Guid employeeId,
        string clientId,
        long nsr,
        PunchDirection direction,
        PunchOrigin origin,
        DateTimeOffset occurredAt,
        DateTimeOffset recordedAt,
        DateOnly localDate,
        TimeSpan companyOffset,
        GeoLocation location,
        DeviceIntegrity integrity)
    {
        CompanyId = companyId;
        EmployeeId = employeeId;
        ClientId = clientId;
        Nsr = nsr;
        Direction = direction;
        Origin = origin;
        OccurredAt = occurredAt;
        RecordedAt = recordedAt;
        LocalDate = localDate;
        CompanyOffset = companyOffset;
        Location = location;
        Integrity = integrity;
        Trust = Classify(origin, location, integrity);
    }

    public Guid CompanyId { get; private set; }

    public Guid EmployeeId { get; private set; }

    /// <summary>
    /// Identifier the device generated before sending.
    /// </summary>
    /// <remarks>
    /// What makes a retry safe. A punch sent, timed out and sent again arrives
    /// twice; without this the employee ends up with two entries and a manager
    /// ends up correcting one. Unique per company.
    /// </remarks>
    public string ClientId { get; private set; }

    /// <summary>
    /// Sequential number within the company, without gaps. Required by the AFD.
    /// </summary>
    public long Nsr { get; private set; }

    public PunchDirection Direction { get; private set; }

    public PunchOrigin Origin { get; private set; }

    /// <summary>The time that counts, in UTC.</summary>
    /// <remarks>
    /// For an online punch this is the server clock. For one that waited in the
    /// device queue it is the device estimate, accepted because the alternative
    /// — stamping it on arrival — would record a punch made at 07:00 as having
    /// happened at 14:00, when the phone finally found signal.
    /// </remarks>
    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>When the server received it. Equal to <see cref="OccurredAt"/> when online.</summary>
    public DateTimeOffset RecordedAt { get; private set; }

    /// <summary>
    /// The calendar day in the company timezone, frozen at creation.
    /// </summary>
    /// <remarks>
    /// Derived once and stored rather than computed on read. Changing the
    /// company timezone later must not silently move past punches into a
    /// different working day.
    /// </remarks>
    public DateOnly LocalDate { get; private set; }

    /// <summary>The company UTC offset at the moment of the punch, also frozen.</summary>
    public TimeSpan CompanyOffset { get; private set; }

    public GeoLocation Location { get; private set; }

    public DeviceIntegrity Integrity { get; private set; }

    public PunchTrust Trust { get; private set; }

    /// <summary>
    /// Records a punch. The NSR and the official time come from the caller
    /// because both belong to the transaction, not to this object.
    /// </summary>
    public static Result<Punch> Register(
        Guid companyId,
        Guid employeeId,
        string clientId,
        long nsr,
        PunchDirection direction,
        PunchOrigin origin,
        DateTimeOffset occurredAt,
        DateTimeOffset recordedAt,
        TimeSpan companyOffset,
        GeoLocation? location,
        DeviceIntegrity integrity)
    {
        ArgumentNullException.ThrowIfNull(integrity);

        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(clientId))
        {
            errors.Add(PunchErrors.ClientIdRequired());
        }

        // The one thing a punch cannot be without. Everything else about the
        // location is classified rather than refused; its absence is malformed
        // input, not a bad reading — a client that sends none did not try.
        if (location is null)
        {
            errors.Add(PunchErrors.LocationRequired());
        }

        if (errors.Count > 0)
        {
            return Result.Failure<Punch>(errors);
        }

        var punch = new Punch(
            companyId,
            employeeId,
            clientId.Trim(),
            nsr,
            direction,
            origin,
            occurredAt,
            recordedAt,
            DateOnly.FromDateTime((occurredAt + companyOffset).DateTime),
            companyOffset,
            location!,
            integrity);

        return Result.Success(punch);
    }

    /// <summary>
    /// Decides how much the record can be relied on, from what the device
    /// reported.
    /// </summary>
    private static PunchTrust Classify(PunchOrigin origin, GeoLocation location, DeviceIntegrity integrity)
    {
        // A mocked position is a deliberate act, and a clock far out of step is
        // the other way to fake a punch. Both go to a human.
        if (integrity.LocationWasMocked || Math.Abs(integrity.ClockSkewSeconds) > SuspiciousClockSkewSeconds)
        {
            return PunchTrust.NeedsReview;
        }

        // Offline is ordinary, not suspicious: it is what the app does in a
        // basement. It lowers what the record proves without implying anything
        // about the person.
        if (origin is PunchOrigin.Offline || location.AccuracyMeters > DegradedAccuracyMeters)
        {
            return PunchTrust.Degraded;
        }

        return PunchTrust.Trusted;
    }
}
