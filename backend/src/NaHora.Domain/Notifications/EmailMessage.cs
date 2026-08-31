using NaHora.Domain.ValueObjects;

namespace NaHora.Domain.Notifications;

/// <summary>
/// One message to one person.
/// </summary>
/// <remarks>
/// Plain text and HTML both travel: a client that cannot render the second
/// shows the first, and a password nobody can read is a password nobody can
/// use.
/// </remarks>
public sealed record EmailMessage(
    EmailAddress To,
    string ToName,
    string Subject,
    string PlainTextBody,
    string HtmlBody);
