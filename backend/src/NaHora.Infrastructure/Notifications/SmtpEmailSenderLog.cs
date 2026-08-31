using Microsoft.Extensions.Logging;

namespace NaHora.Infrastructure.Notifications;

/// <summary>
/// Source-generated log messages for outgoing mail.
/// </summary>
/// <remarks>
/// The subject travels, the body never does. The body is what carries the
/// credential the message exists to deliver, and a log is the last place it
/// should end up.
/// </remarks>
internal static partial class SmtpEmailSenderLog
{
    [LoggerMessage(
        EventId = 3001,
        Level = LogLevel.Information,
        Message = "Email sent: {Subject}")]
    public static partial void Sent(ILogger logger, string subject);

    [LoggerMessage(
        EventId = 3002,
        Level = LogLevel.Error,
        Message = "Email could not be sent: {Subject}. The work it belongs to was not rolled back.")]
    public static partial void Failed(ILogger logger, string subject, Exception exception);
}
