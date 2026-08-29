namespace FaceId.Domain.Abstractions;

/// <summary>
/// Entry point of an aggregate, and the boundary of one transaction.
/// </summary>
/// <remarks>
/// The distinction is visible in the class declaration itself, so reading
/// <c>: AggregateRoot</c> versus <c>: Entity</c> answers the question without
/// opening anything else:
///
/// <code>
/// public sealed class Employee : AggregateRoot   // loaded and saved on its own
/// public sealed class FaceEmbedding : Entity     // only ever reached through its root
/// </code>
///
/// What follows from being a root:
/// <list type="bullet">
/// <item>Only a root gets a repository. A child is loaded and saved through
/// its root, never on its own.</item>
/// <item>Only a root is referenced by identifier from outside its aggregate.
/// A reference to a child from another aggregate is a modelling error.</item>
/// <item>The root enforces the invariants that span its children, which is why
/// the whole aggregate commits together.</item>
/// </list>
///
/// When in doubt, an entity is not a root. Wrongly promoting one splits a
/// consistency boundary that the domain expected to hold.
/// </remarks>
public abstract class AggregateRoot : Entity;
