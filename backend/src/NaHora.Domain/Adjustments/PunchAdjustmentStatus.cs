namespace NaHora.Domain.Adjustments;

/// <summary>
/// Where a treatment stands.
/// </summary>
/// <remarks>
/// Only <see cref="Approved"/> changes what counts. A pending request is
/// visible to everybody involved and affects nothing, which is what lets an
/// employee ask without being able to rewrite their own record.
/// </remarks>
public enum PunchAdjustmentStatus
{
    /// <summary>Asked for, waiting on a manager.</summary>
    Pending = 0,

    /// <summary>A manager agreed. This is the time that now counts.</summary>
    Approved = 1,

    /// <summary>A manager declined. Kept, because the request is part of the trail.</summary>
    Rejected = 2
}
