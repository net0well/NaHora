using NaHora.Domain.Abstractions;

namespace NaHora.Domain.Adjustments;

/// <summary>
/// One reason a manager may give for correcting a punch.
/// </summary>
/// <remarks>
/// <para>
/// A root rather than a child: a punch adjustment references it by identifier
/// from outside its own aggregate, and only a root may be referenced that way.
/// It gets no repository, because nothing loads one on its own — the catalogue
/// is read whole.
/// </para>
/// <para>
/// <b>Not tenant-owned, and not editable by the customer.</b> The catalogue is
/// fixed and seeded in a migration. A company that could write its own reasons
/// would end up with a single entry reading "ajuste", which serves neither an
/// inspection nor its own audit.
/// </para>
/// </remarks>
public sealed class AdjustmentJustification : AggregateRoot
{
    private AdjustmentJustification()
    {
        Code = string.Empty;
        Description = string.Empty;
    }

    internal AdjustmentJustification(
        Guid id,
        string code,
        string description,
        bool requiresNotes,
        int displayOrder)
    {
        Id = id;
        Code = code;
        Description = description;
        RequiresNotes = requiresNotes;
        DisplayOrder = displayOrder;
    }

    /// <summary>
    /// Stable identifier in SCREAMING_SNAKE_CASE. The contract with the app,
    /// which translates on it; the description may be reworded freely.
    /// </summary>
    public string Code { get; private set; }

    /// <summary>What a manager reads when choosing. Portuguese, like all user-facing text.</summary>
    public string Description { get; private set; }

    /// <summary>
    /// Whether picking this one obliges the manager to write something.
    /// </summary>
    /// <remarks>
    /// True only for the catch-all. Without it, "Outro" becomes the shortcut
    /// for not choosing and the whole catalogue stops meaning anything.
    /// </remarks>
    public bool RequiresNotes { get; private set; }

    /// <summary>
    /// Order in the picker, so the common reasons are not buried alphabetically
    /// under the rare ones.
    /// </summary>
    public int DisplayOrder { get; private set; }
}
