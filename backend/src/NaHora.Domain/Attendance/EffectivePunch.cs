using NaHora.Domain.Adjustments;

namespace NaHora.Domain.Attendance;

/// <summary>
/// One mark of the working day as it actually counts, after approved
/// treatments.
/// </summary>
/// <remarks>
/// <para>
/// Not an entity and never persisted. <see cref="Punch"/> is append-only and
/// stays exactly as recorded — the row goes signed into the AFD and rewriting
/// it would break the chain. This is the reading of the record, assembled every
/// time somebody asks for it.
/// </para>
/// <para>
/// Several properties are null for an inclusion, and that is the point: nothing
/// was captured, so there is no NSR, no origin, no trust and no accuracy. Filling
/// them with defaults would present a manager's decision as a measurement.
/// </para>
/// </remarks>
public sealed record EffectivePunch
{
    private EffectivePunch(
        Guid id,
        long? nsr,
        PunchDirection direction,
        PunchOrigin? origin,
        PunchTrust? trust,
        DateTimeOffset occurredAt,
        DateTimeOffset? originalOccurredAt,
        DateOnly localDate,
        TimeSpan companyOffset,
        double? accuracyMeters,
        Guid? adjustmentId,
        EffectivePunchSource source)
    {
        Id = id;
        Nsr = nsr;
        Direction = direction;
        Origin = origin;
        Trust = trust;
        OccurredAt = occurredAt;
        OriginalOccurredAt = originalOccurredAt;
        LocalDate = localDate;
        CompanyOffset = companyOffset;
        AccuracyMeters = accuracyMeters;
        AdjustmentId = adjustmentId;
        Source = source;
    }

    /// <summary>
    /// The punch, or the treatment when there is no punch behind it.
    /// </summary>
    /// <remarks>
    /// Identifies the row on screen. An inclusion borrows its treatment's
    /// identifier because it has nothing else, and the two can never collide:
    /// they come from different tables.
    /// </remarks>
    public Guid Id { get; }

    /// <summary>
    /// The sequential number in the company's record — what an inspection
    /// cites. Null for an inclusion, which no registrar ever numbered.
    /// </summary>
    public long? Nsr { get; }

    public PunchDirection Direction { get; }

    /// <summary>Null for an inclusion: nothing was captured, online or offline.</summary>
    public PunchOrigin? Origin { get; }

    /// <summary>Null for an inclusion: there was no capture to trust or doubt.</summary>
    public PunchTrust? Trust { get; }

    /// <summary>The time that governs.</summary>
    public DateTimeOffset OccurredAt { get; }

    /// <summary>
    /// The time this replaced, when a correction replaced one.
    /// </summary>
    /// <remarks>
    /// Kept beside the new value rather than dropped. A correction that hides
    /// what it replaced is an edit, and the employee has the right to see that
    /// their record was changed and from what.
    /// </remarks>
    public DateTimeOffset? OriginalOccurredAt { get; }

    /// <summary>The working day this belongs to, in company time.</summary>
    public DateOnly LocalDate { get; }

    public TimeSpan CompanyOffset { get; }

    /// <summary>Null for an inclusion: no position was ever measured.</summary>
    public double? AccuracyMeters { get; }

    /// <summary>The treatment that decided this time, when one did.</summary>
    public Guid? AdjustmentId { get; }

    public EffectivePunchSource Source { get; }

    /// <summary>A mark exactly as the registrar captured it.</summary>
    public static EffectivePunch FromPunch(Punch punch)
    {
        ArgumentNullException.ThrowIfNull(punch);

        return new EffectivePunch(
            punch.Id,
            punch.Nsr,
            punch.Direction,
            punch.Origin,
            punch.Trust,
            punch.OccurredAt,
            originalOccurredAt: null,
            punch.LocalDate,
            punch.CompanyOffset,
            punch.Location.AccuracyMeters,
            adjustmentId: null,
            EffectivePunchSource.Recorded);
    }

    /// <summary>A mark whose time an approved treatment replaced.</summary>
    /// <remarks>
    /// Everything except the time stays the punch's own. The capture happened;
    /// only the moment it is taken to have happened at changed, so origin,
    /// trust and accuracy still describe that capture.
    ///
    /// The working day is recomputed, because a correction can move a mark
    /// across midnight and leaving it filed under the old day would show the
    /// employee a day that no longer matches their hours.
    /// </remarks>
    public static EffectivePunch Corrected(Punch punch, PunchAdjustment treatment)
    {
        ArgumentNullException.ThrowIfNull(punch);
        ArgumentNullException.ThrowIfNull(treatment);

        return new EffectivePunch(
            punch.Id,
            punch.Nsr,
            punch.Direction,
            punch.Origin,
            punch.Trust,
            treatment.AdjustedOccurredAt,
            treatment.OriginalOccurredAt ?? punch.OccurredAt,
            LocalDateOf(treatment.AdjustedOccurredAt, punch.CompanyOffset),
            punch.CompanyOffset,
            punch.Location.AccuracyMeters,
            treatment.Id,
            EffectivePunchSource.Adjusted);
    }

    /// <summary>A mark that was never captured and counts because it was approved.</summary>
    public static EffectivePunch Included(PunchAdjustment treatment, TimeSpan companyOffset)
    {
        ArgumentNullException.ThrowIfNull(treatment);

        return new EffectivePunch(
            treatment.Id,
            nsr: null,
            treatment.Direction,
            origin: null,
            trust: null,
            treatment.AdjustedOccurredAt,
            originalOccurredAt: null,
            LocalDateOf(treatment.AdjustedOccurredAt, companyOffset),
            companyOffset,
            accuracyMeters: null,
            treatment.Id,
            EffectivePunchSource.Included);
    }

    private static DateOnly LocalDateOf(DateTimeOffset instant, TimeSpan companyOffset) =>
        DateOnly.FromDateTime((instant + companyOffset).DateTime);
}
