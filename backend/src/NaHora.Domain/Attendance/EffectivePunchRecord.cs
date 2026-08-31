using NaHora.Domain.Adjustments;

namespace NaHora.Domain.Attendance;

/// <summary>
/// Reads the record as it counts: the marks captured, plus what approved
/// treatments did to them.
/// </summary>
/// <remarks>
/// <para>
/// Pure, and in the domain because more than one screen asks the same question
/// — the employee's day, their history, and eventually the manager's view of
/// somebody else's. Two copies would drift, and the drift shows up as an
/// approved correction that counts on one screen and not on the other.
/// </para>
/// <para>
/// Only an approved treatment changes anything. A pending request is somebody
/// asking; a refused one is somebody having been told no. Neither may move a
/// time, and both stay in the record either way.
/// </para>
/// </remarks>
public static class EffectivePunchRecord
{
    /// <summary>
    /// Merges captured marks with the treatments that govern them, ordered by
    /// the time that counts.
    /// </summary>
    /// <param name="companyOffsetAt">
    /// The company offset in force at an instant. Asked per inclusion rather
    /// than passed once: an inclusion granted for a day months back must render
    /// at the offset of that day, not of today.
    ///
    /// Only inclusions need it. A correction keeps the offset of the punch it
    /// corrects, which the punch carries itself.
    /// </param>
    public static IReadOnlyList<EffectivePunch> Merge(
        IReadOnlyCollection<Punch> punches,
        IReadOnlyCollection<PunchAdjustment> treatments,
        Func<DateTimeOffset, TimeSpan> companyOffsetAt)
    {
        ArgumentNullException.ThrowIfNull(punches);
        ArgumentNullException.ThrowIfNull(treatments);
        ArgumentNullException.ThrowIfNull(companyOffsetAt);

        var governing = GoverningCorrections(treatments);

        var corrected = punches.Select(punch =>
            governing.TryGetValue(punch.Id, out var treatment)
                ? EffectivePunch.Corrected(punch, treatment)
                : EffectivePunch.FromPunch(punch));

        var included = treatments
            .Where(treatment => treatment.Counts && treatment.PunchId is null)
            .Select(treatment => EffectivePunch.Included(
                treatment,
                companyOffsetAt(treatment.AdjustedOccurredAt)));

        return [.. corrected.Concat(included).OrderBy(entry => entry.OccurredAt)];
    }

    /// <summary>
    /// The approved correction that decides each punch's time.
    /// </summary>
    /// <remarks>
    /// One punch can carry more than one approved correction: nothing stops an
    /// employee asking twice and a manager agreeing twice. The latest decision
    /// governs, because that is the one taken with the earlier one already in
    /// view — and both stay in the record.
    /// </remarks>
    private static Dictionary<Guid, PunchAdjustment> GoverningCorrections(
        IReadOnlyCollection<PunchAdjustment> treatments) =>
        treatments
            .Where(treatment => treatment.Counts && treatment.PunchId is not null)
            .GroupBy(treatment => treatment.PunchId!.Value)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(treatment => treatment.DecidedAt)
                    .First());
}
