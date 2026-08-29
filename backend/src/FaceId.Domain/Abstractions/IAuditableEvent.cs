namespace FaceId.Domain.Abstractions;

/// <summary>
/// A domain event that has to be recorded, and therefore travels on the durable
/// queue.
/// </summary>
/// <remarks>
/// This is a routing contract, not a behaviour: implementing it is what puts an
/// event on the queue whose delivery is durable, retried and consumed exactly
/// once. Events that may be dropped — a live update to an open screen — must
/// not implement it, because paying for durability to deliver something nobody
/// will be looking at in ten seconds is waste.
///
/// A marker rather than a namespace convention on purpose: routing by namespace
/// string means a rename stops delivery and nothing fails to compile.
///
/// Not to be confused with <see cref="AuditAttribute"/>, which marks an
/// <em>entity</em> whose persistence is written to the trail.
/// </remarks>
public interface IAuditableEvent;
