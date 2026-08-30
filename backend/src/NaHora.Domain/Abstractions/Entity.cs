namespace NaHora.Domain.Abstractions;

/// <summary>
/// Base for persisted entities. Identity is the key alone, so two instances
/// loaded through different queries compare equal.
/// </summary>
public abstract class Entity : IEquatable<Entity>
{
    /// <summary>
    /// Version 7 GUID: time-ordered, which keeps index locality on insert
    /// instead of scattering writes across the B-tree like a random GUID does.
    /// </summary>
    public Guid Id { get; protected init; } = Guid.CreateVersion7();

    public bool Equals(Entity? other)
    {
        if (other is null)
        {
            return false;
        }

        return ReferenceEquals(this, other) || (GetType() == other.GetType() && Id == other.Id);
    }

    public override bool Equals(object? obj) => obj is Entity entity && Equals(entity);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity? left, Entity? right) => Equals(left, right);

    public static bool operator !=(Entity? left, Entity? right) => !Equals(left, right);
}
