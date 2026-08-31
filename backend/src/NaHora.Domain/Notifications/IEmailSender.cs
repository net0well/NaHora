namespace NaHora.Domain.Notifications;

/// <summary>
/// Delivers an email.
/// </summary>
/// <remarks>
/// A port so the domain can say "tell this person their password" without
/// knowing whether that travels over SMTP, a transactional API, or a log line
/// in development.
/// </remarks>
public interface IEmailSender
{
    /// <summary>
    /// Sends the message.
    /// </summary>
    /// <remarks>
    /// Returns whether it went out rather than throwing. A mail server that is
    /// down must not roll back a hire: the employment record is the thing with
    /// legal weight, and an undelivered password is recoverable by resetting
    /// it.
    /// </remarks>
    Task<bool> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
