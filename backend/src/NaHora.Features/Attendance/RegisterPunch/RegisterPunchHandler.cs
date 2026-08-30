using NaHora.Domain.Abstractions;
using NaHora.Domain.Attendance;
using NaHora.Domain.Attendance.Events;
using NaHora.Domain.Companies;
using NaHora.Domain.Employees;
using NaHora.Domain.Errors;
using NaHora.Shared.Results;
using Wolverine;

namespace NaHora.Features.Attendance.RegisterPunch;

/// <summary>
/// Records a punch.
/// </summary>
/// <remarks>
/// The entry point reads as the questions recording a punch actually asks: is
/// this someone who punches, has this punch already arrived, when did it
/// happen, and which boundary of the day does it mark.
///
/// Nothing here refuses a punch for looking suspicious. Art. 78 of Portaria 671
/// forbids the registrar from restricting the record, so a mocked location or a
/// wrong device clock changes the classification and never the outcome.
/// </remarks>
public static class RegisterPunchHandler
{
    public static async Task<Result<RegisterPunchResponse>> Handle(
        RegisterPunchCommand command,
        ICurrentUser currentUser,
        IPunchRepository punches,
        IEmployeeRepository employees,
        ICompanyRepository companies,
        IMessageBus bus,
        IUnitOfWork unitOfWork,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(currentUser);

        var companyId = currentUser.CompanyId;

        var employee = await employees.GetByUserIdAsync(companyId, currentUser.UserId, cancellationToken);
        if (employee is null || employee.Status is not EmployeeStatus.Active)
        {
            return Result.Failure<RegisterPunchResponse>(PunchErrors.EmployeeRequired());
        }

        // Asked before anything is written. A client that retried after a
        // timeout must get its original punch back, not a second one.
        var existing = await punches.GetByClientIdAsync(companyId, command.ClientId ?? string.Empty, cancellationToken);
        if (existing is not null)
        {
            return Result.Success(BuildResponse(existing, alreadyRegistered: true));
        }

        var company = await companies.GetByIdAsync(companyId, cancellationToken);
        if (company is null)
        {
            return Result.Failure<RegisterPunchResponse>(CompanyErrors.NotFound());
        }

        var recordedAt = clock.GetUtcNow();
        var occurredAt = ResolveOfficialTime(command.Offline, recordedAt);
        var origin = command.Offline is null ? PunchOrigin.Online : PunchOrigin.Offline;

        var locationResult = BuildLocation(command.Location);
        if (locationResult.IsFailure)
        {
            return Result.Failure<RegisterPunchResponse>(locationResult.Errors);
        }

        var direction = await ResolveNextDirectionAsync(
            punches, companyId, employee.Id, occurredAt, company.TimeZoneId, cancellationToken);

        // Taken last, and inside the same transaction as the insert: a number
        // handed out by a transaction that then fails would leave a gap the AFD
        // cannot have.
        var nsr = await punches.TakeNextNsrAsync(companyId, cancellationToken);

        var punchResult = Punch.Register(
            companyId,
            employee.Id,
            command.ClientId ?? string.Empty,
            nsr,
            direction,
            origin,
            occurredAt,
            recordedAt,
            ResolveOffset(company.TimeZoneId, occurredAt),
            locationResult.Value,
            BuildIntegrity(command.Device, occurredAt));

        if (punchResult.IsFailure)
        {
            return Result.Failure<RegisterPunchResponse>(punchResult.Errors);
        }

        var punch = punchResult.Value;
        punches.Add(punch);

        await bus.PublishAsync(new PunchRegistered(
            companyId,
            employee.Id,
            punch.Id,
            punch.Nsr,
            punch.Direction.ToString(),
            punch.Trust.ToString(),
            punch.OccurredAt));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(BuildResponse(punch, alreadyRegistered: false));
    }

    /// <summary>
    /// The moment that counts.
    /// </summary>
    /// <remarks>
    /// Online, it is the server's own clock and the client has no say. Offline,
    /// it is the device's estimate, because there was no server to ask at the
    /// time and stamping arrival would move the punch by however long the phone
    /// lacked signal.
    /// </remarks>
    private static DateTimeOffset ResolveOfficialTime(OfflinePunchInput? offline, DateTimeOffset recordedAt) =>
        offline is null ? recordedAt : offline.OccurredAtEstimate;

    private static Result<GeoLocation> BuildLocation(PunchLocationInput? input) =>
        input is null
            ? Result.Failure<GeoLocation>(PunchErrors.LocationRequired())
            : GeoLocation.Create(input.Latitude, input.Longitude, input.AccuracyMeters, input.CapturedAt);

    /// <summary>
    /// Builds the integrity record, tolerating a client that sent nothing.
    /// </summary>
    /// <remarks>
    /// A missing device block is treated as an unknown device rather than a
    /// rejected punch — refusing here would be exactly the restriction Art. 78
    /// forbids, and an old app version is not fraud.
    /// </remarks>
    private static DeviceIntegrity BuildIntegrity(PunchDeviceInput? input, DateTimeOffset officialTime) =>
        input is null
            ? DeviceIntegrity.Create("unknown", locationWasMocked: false, officialTime, officialTime)
            : DeviceIntegrity.Create(input.DeviceId, input.LocationWasMocked, input.DeviceClockAt, officialTime);

    /// <summary>
    /// Decides which boundary of the working day this punch marks, from the
    /// ones already recorded that day.
    /// </summary>
    /// <remarks>
    /// The server decides, not the app. Letting the client choose would allow
    /// two entries in a row, and the correction lands on a manager.
    /// </remarks>
    private static async Task<PunchDirection> ResolveNextDirectionAsync(
        IPunchRepository punches,
        Guid companyId,
        Guid employeeId,
        DateTimeOffset occurredAt,
        string timeZoneId,
        CancellationToken cancellationToken)
    {
        var localDate = DateOnly.FromDateTime((occurredAt + ResolveOffset(timeZoneId, occurredAt)).DateTime);

        var today = await punches.GetByLocalDateAsync(companyId, employeeId, localDate, cancellationToken);

        // Entry, break out, break in, exit — then round again for a split
        // shift, which is common enough in Brazil to be the default rather
        // than an edge case.
        return (today.Count % 4) switch
        {
            0 => PunchDirection.Entry,
            1 => PunchDirection.BreakStart,
            2 => PunchDirection.BreakEnd,
            _ => PunchDirection.Exit
        };
    }

    /// <summary>
    /// The company's UTC offset at that instant.
    /// </summary>
    /// <remarks>
    /// Resolved for the moment of the punch rather than for now, so a punch
    /// made before a daylight-saving change keeps the offset that was in force
    /// when it happened.
    /// </remarks>
    private static TimeSpan ResolveOffset(string timeZoneId, DateTimeOffset instant)
    {
        if (!TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out var zone))
        {
            return TimeSpan.Zero;
        }

        return zone.GetUtcOffset(instant);
    }

    private static RegisterPunchResponse BuildResponse(Punch punch, bool alreadyRegistered) =>
        new(
            punch.Id,
            punch.Nsr,
            punch.Direction.ToString(),
            punch.Origin.ToString(),
            punch.Trust.ToString(),
            punch.OccurredAt,
            punch.LocalDate,
            alreadyRegistered);
}
