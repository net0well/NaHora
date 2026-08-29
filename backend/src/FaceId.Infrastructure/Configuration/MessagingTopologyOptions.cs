using System.Globalization;

namespace FaceId.Infrastructure.Configuration;

/// <summary>
/// The broker topology: one queue per kind of work.
/// </summary>
/// <remarks>
/// A single queue carrying everything is the arrangement this replaces. It
/// makes independent things share a fate: a poison message parks the queue and
/// stops deliveries that had nothing to do with it, a slow consumer adds
/// latency to every other consumer, and the depth metric answers no useful
/// question because it mixes work that should be instant with work that may
/// take a minute.
///
/// Split by <em>delivery contract</em> rather than by feature, because that is
/// what forces different infrastructure:
///
/// <list type="bullet">
/// <item><b>Audit</b> — durable and consumed once. The record must survive a
/// restart, so the queue outlives the process and instances compete for
/// messages.</item>
/// <item><b>Real time</b> — ephemeral and delivered to everyone. Every instance
/// needs its own copy to reach its own observers, so each declares a private
/// queue that dies with it. A message nobody is connected to see is worth
/// nothing a second later, so none of it is durable.</item>
/// </list>
/// </remarks>
public sealed class MessagingTopologyOptions
{
    /// <summary>Fans domain events out to the durable audit queue.</summary>
    public string AuditExchange { get; init; } = "faceid.audit";

    /// <summary>
    /// Durable, shared by every instance. Competing consumers, because an audit
    /// record written once per instance would be an audit record written twice.
    /// </summary>
    public string AuditQueue { get; init; } = "faceid.audit";

    /// <summary>Fans live updates out to every instance.</summary>
    public string RealTimeExchange { get; init; } = "faceid.realtime";

    /// <summary>
    /// Prefix of this instance's private queue. The suffix makes it unique, and
    /// the queue is exclusive and auto-deleting: it exists while the instance
    /// does and disappears with it, leaving nothing to clean up after a crash.
    /// </summary>
    public string RealTimeQueuePrefix { get; init; } = "faceid.realtime";

    /// <summary>
    /// This instance's real-time queue name.
    /// </summary>
    /// <remarks>
    /// Machine and process make it identifiable in the broker's console, which
    /// matters when the question is "which instance is not delivering".
    /// </remarks>
    public string ResolveRealTimeQueue() => string.Format(
        CultureInfo.InvariantCulture,
        "{0}.{1}.{2}",
        RealTimeQueuePrefix,
        Environment.MachineName.ToLowerInvariant(),
        Environment.ProcessId);
}
