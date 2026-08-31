using NaHora.Domain.Abstractions;
using NaHora.Domain.Attendance;
using NaHora.Domain.Errors;
using NaHora.Shared.Results;

namespace NaHora.Domain.Adjustments;

/// <summary>
/// A correction to somebody's time record.
/// </summary>
/// <remarks>
/// <para>
/// <b>Never an edit.</b> The punch it points at keeps every field it was
/// recorded with — that row goes signed into the AFD, and rewriting it would
/// invalidate the chain rather than fix a line. This carries the time that
/// should count instead, next to the one that was recorded.
/// </para>
/// <para>
/// <b>A request, then a decision.</b> The employee whose record it is may ask;
/// only a manager may agree. That split is the whole point: an employee who
/// could approve their own correction is an employee editing their own
/// timesheet, which is the most obvious conflict of interest in this domain.
/// </para>
/// <para>
/// A manager acting on somebody's record does both at once — they already hold
/// the authority the second step exists to check, and making them approve their
/// own action would be ceremony rather than control.
/// </para>
/// </remarks>
[Audit]
public sealed class PunchAdjustment : AggregateRoot, ITenantOwned
{
    private PunchAdjustment()
    {
    }

    private PunchAdjustment(
        Guid companyId,
        Guid employeeId,
        Guid? punchId,
        PunchAdjustmentKind kind,
        DateTimeOffset? originalOccurredAt,
        DateTimeOffset adjustedOccurredAt,
        PunchDirection direction,
        Guid justificationId,
        string? notes,
        Guid requestedByUserId,
        DateTimeOffset requestedAt)
    {
        CompanyId = companyId;
        EmployeeId = employeeId;
        PunchId = punchId;
        Kind = kind;
        OriginalOccurredAt = originalOccurredAt;
        AdjustedOccurredAt = adjustedOccurredAt;
        Direction = direction;
        JustificationId = justificationId;
        Notes = notes;
        RequestedByUserId = requestedByUserId;
        RequestedAt = requestedAt;
        Status = PunchAdjustmentStatus.Pending;
    }

    public Guid CompanyId { get; private set; }

    /// <summary>Whose record this touches.</summary>
    public Guid EmployeeId { get; private set; }

    /// <summary>The punch being corrected. Null when nothing was recorded.</summary>
    public Guid? PunchId { get; private set; }

    public PunchAdjustmentKind Kind { get; private set; }

    /// <summary>
    /// What the punch actually recorded, copied at the moment the treatment was
    /// asked for.
    /// </summary>
    /// <remarks>
    /// Copied rather than read through the reference so the trail shows what
    /// was being corrected even if the reference is later followed to a record
    /// somebody has since read differently.
    /// </remarks>
    public DateTimeOffset? OriginalOccurredAt { get; private set; }

    /// <summary>The time that counts once this is approved.</summary>
    public DateTimeOffset AdjustedOccurredAt { get; private set; }

    public PunchDirection Direction { get; private set; }

    /// <summary>From the fixed catalogue. Free text is not a justification.</summary>
    public Guid JustificationId { get; private set; }

    public string? Notes { get; private set; }

    /// <summary>Object key in storage. Null when nothing was attached.</summary>
    public string? AttachmentKey { get; private set; }

    public PunchAdjustmentStatus Status { get; private set; }

    public Guid RequestedByUserId { get; private set; }

    public DateTimeOffset RequestedAt { get; private set; }

    public Guid? DecidedByUserId { get; private set; }

    public DateTimeOffset? DecidedAt { get; private set; }

    public string? DecisionNotes { get; private set; }

    public bool IsPending => Status == PunchAdjustmentStatus.Pending;

    /// <summary>Whether this is the treatment that decides the effective time.</summary>
    public bool Counts => Status == PunchAdjustmentStatus.Approved;

    /// <summary>
    /// Asks for a correction to an existing punch.
    /// </summary>
    public static Result<PunchAdjustment> RequestTimeChange(
        Guid companyId,
        Guid employeeId,
        Punch punch,
        DateTimeOffset adjustedOccurredAt,
        AdjustmentJustification? justification,
        string? notes,
        Guid requestedByUserId,
        DateTimeOffset requestedAt)
    {
        ArgumentNullException.ThrowIfNull(punch);

        var errors = Validate(justification, notes, requestedByUserId);

        if (errors.Count > 0)
        {
            return Result.Failure<PunchAdjustment>(errors);
        }

        return Result.Success(new PunchAdjustment(
            companyId,
            employeeId,
            punch.Id,
            PunchAdjustmentKind.TimeChange,
            punch.OccurredAt,
            adjustedOccurredAt,
            punch.Direction,
            justification!.Id,
            Trim(notes),
            requestedByUserId,
            requestedAt));
    }

    /// <summary>
    /// Asks for a punch that was never recorded to count.
    /// </summary>
    public static Result<PunchAdjustment> RequestInclusion(
        Guid companyId,
        Guid employeeId,
        DateTimeOffset occurredAt,
        PunchDirection direction,
        AdjustmentJustification? justification,
        string? notes,
        Guid requestedByUserId,
        DateTimeOffset requestedAt)
    {
        var errors = Validate(justification, notes, requestedByUserId);

        // A correction cannot be for the future: nothing has happened yet to
        // correct, and a time nobody has reached is a schedule, not a record.
        if (occurredAt > requestedAt)
        {
            errors.Add(AdjustmentErrors.OccurredAtInTheFuture());
        }

        if (errors.Count > 0)
        {
            return Result.Failure<PunchAdjustment>(errors);
        }

        return Result.Success(new PunchAdjustment(
            companyId,
            employeeId,
            punchId: null,
            PunchAdjustmentKind.Inclusion,
            originalOccurredAt: null,
            occurredAt,
            direction,
            justification!.Id,
            Trim(notes),
            requestedByUserId,
            requestedAt));
    }

    /// <summary>
    /// Attaches supporting evidence — a medical certificate, most often.
    /// </summary>
    /// <remarks>
    /// Sent after the treatment is recorded rather than with it. The treatment
    /// is the legal artefact and has to survive an upload that fails.
    /// </remarks>
    public Result Attach(string objectKey)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
        {
            return Result.Failure(AdjustmentErrors.AttachmentKeyRequired());
        }

        if (!IsPending)
        {
            return Result.Failure(AdjustmentErrors.AlreadyDecided());
        }

        AttachmentKey = objectKey.Trim();

        return Result.Success();
    }

    /// <summary>
    /// A manager agrees, and from here the adjusted time is the one that
    /// counts.
    /// </summary>
    public Result Approve(
        Guid managerUserId,
        Guid? managerEmployeeId,
        string? decisionNotes,
        DateTimeOffset decidedAt)
    {
        var guard = GuardDecision(managerEmployeeId);
        if (guard.IsFailure)
        {
            return guard;
        }

        Status = PunchAdjustmentStatus.Approved;
        DecidedByUserId = managerUserId;
        DecidedAt = decidedAt;
        DecisionNotes = Trim(decisionNotes);

        return Result.Success();
    }

    /// <summary>
    /// A manager declines. The request is kept: it is part of the trail, and
    /// deleting it would hide that somebody asked.
    /// </summary>
    public Result Reject(
        Guid managerUserId,
        Guid? managerEmployeeId,
        string? decisionNotes,
        DateTimeOffset decidedAt)
    {
        var guard = GuardDecision(managerEmployeeId);
        if (guard.IsFailure)
        {
            return guard;
        }

        Status = PunchAdjustmentStatus.Rejected;
        DecidedByUserId = managerUserId;
        DecidedAt = decidedAt;
        DecisionNotes = Trim(decisionNotes);

        return Result.Success();
    }

    /// <summary>
    /// Whether this person may decide, given whose record it is.
    /// </summary>
    /// <remarks>
    /// A manager deciding on somebody else's record decides alone — spec 014
    /// deferred two-step approval on purpose, and a second signature would be
    /// ceremony rather than control.
    ///
    /// Their own record is the exception, and the reason the split exists at
    /// all: approving a correction to your own timesheet is editing your own
    /// timesheet with extra steps.
    /// </remarks>
    private Result GuardDecision(Guid? managerEmployeeId)
    {
        if (!IsPending)
        {
            return Result.Failure(AdjustmentErrors.AlreadyDecided());
        }

        if (managerEmployeeId is { } decider && decider == EmployeeId)
        {
            return Result.Failure(AdjustmentErrors.CannotDecideOwnRecord());
        }

        return Result.Success();
    }

    private static List<Error> Validate(AdjustmentJustification? justification, string? notes, Guid requestedByUserId)
    {
        var errors = new List<Error>();

        if (justification is null)
        {
            errors.Add(AdjustmentErrors.JustificationRequired());
        }
        else if (justification.RequiresNotes && string.IsNullOrWhiteSpace(notes))
        {
            // Without this, the catch-all becomes the way to avoid choosing and
            // the catalogue stops distinguishing anything.
            errors.Add(AdjustmentErrors.JustificationNotesRequired());
        }

        if (requestedByUserId == Guid.Empty)
        {
            errors.Add(AdjustmentErrors.RequesterRequired());
        }

        return errors;
    }

    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
