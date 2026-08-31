using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using NaHora.Domain.Notifications;
using NaHora.Shared.DependencyInjection;

namespace NaHora.Infrastructure.Notifications;

/// <summary>
/// Sends mail over SMTP.
/// </summary>
/// <remarks>
/// <para>
/// Named for the technology it adapts rather than for the pattern: swapping it
/// for a transactional API means a new class with a new name, and reading
/// <c>SmtpEmailSender</c> tells you what changes.
/// </para>
/// <para>
/// Public so Wolverine's generated handler code can construct it directly.
/// </para>
/// <para>
/// <b>A failure here is reported, never thrown.</b> A mail server that is down
/// must not roll back a hire — the employment record is the thing with legal
/// weight, and a password that did not arrive is fixed by resetting it.
/// </para>
/// </remarks>
public sealed class SmtpEmailSender(
    IOptions<SmtpOptions> options,
    ILogger<SmtpEmailSender> logger) : IEmailSender, IScopedService
{
    public async Task<bool> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var settings = options.Value;

        try
        {
            using var mime = Build(message, settings);
            using var client = new SmtpClient();

            await client.ConnectAsync(
                settings.Host,
                settings.Port,
                settings.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.None,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(settings.Username))
            {
                // Empty rather than null: a server configured with a user and
                // no password is a misconfiguration to surface, not a crash.
                await client.AuthenticateAsync(
                    settings.Username,
                    settings.Password ?? string.Empty,
                    cancellationToken);
            }

            await client.SendAsync(mime, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            SmtpEmailSenderLog.Sent(logger, message.Subject);

            return true;
        }
#pragma warning disable CA1031 // Any failure to deliver is reported, never propagated — see the type remarks.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            // The subject, never the body: the body is what carries the
            // password we were trying to deliver.
            SmtpEmailSenderLog.Failed(logger, message.Subject, exception);

            return false;
        }
    }

    private static MimeMessage Build(EmailMessage message, SmtpOptions settings)
    {
        var mime = new MimeMessage
        {
            Subject = message.Subject
        };

        mime.From.Add(new MailboxAddress(settings.FromName, settings.FromAddress));
        mime.To.Add(new MailboxAddress(message.ToName, message.To.Value));

        // Both parts, so a client that cannot render HTML still shows something
        // the person can act on.
        mime.Body = new MultipartAlternative
        {
            new TextPart(TextFormat.Plain) { Text = message.PlainTextBody },
            new TextPart(TextFormat.Html) { Text = message.HtmlBody }
        };

        return mime;
    }
}
