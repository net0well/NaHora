using System.Collections.Concurrent;
using NaHora.Domain.Notifications;

namespace NaHora.IntegrationTests.Fixtures;

/// <summary>
/// Captures outgoing mail instead of sending it.
/// </summary>
/// <remarks>
/// Keeps the suite off the network, and lets a test read the password that was
/// sent — which is the only place it exists, by design.
///
/// Public with a parameterless constructor so Wolverine's generated handler
/// code can build it, like the sender it replaces.
/// </remarks>
public sealed class StubEmailSender : IEmailSender
{
    private static readonly ConcurrentBag<EmailMessage> SentMessages = [];

    /// <summary>Set false to make delivery fail, without an exception.</summary>
    public static bool Delivers { get; set; } = true;

    public static IReadOnlyCollection<EmailMessage> Sent => SentMessages;

    public static EmailMessage? LastTo(string address) =>
        SentMessages
            .Where(message => message.To.Value.Equals(address, StringComparison.OrdinalIgnoreCase))
            .LastOrDefault();

    /// <summary>
    /// Restores delivery, and deliberately keeps what was already sent.
    /// </summary>
    /// <remarks>
    /// Test classes run in parallel and share this bag. Clearing it here wiped
    /// messages another class was about to read, which failed as a null
    /// reference far away from the cause. Every test addresses a unique
    /// mailbox, so the messages piling up cost nothing.
    /// </remarks>
    public static void ResetDelivery() => Delivers = true;

    public Task<bool> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        SentMessages.Add(message);

        return Task.FromResult(Delivers);
    }
}
