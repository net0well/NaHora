using System.ComponentModel.DataAnnotations;

namespace NaHora.Infrastructure.Notifications;

/// <summary>
/// Where outgoing mail goes.
/// </summary>
/// <remarks>
/// In development this points at Mailpit, which accepts anything and shows it
/// in a browser — so a developer can read the password an employee was sent
/// without a real mailbox and without the message ever leaving the machine.
/// </remarks>
public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    [Required]
    public string Host { get; init; } = "localhost";

    [Range(1, 65535)]
    public int Port { get; init; } = 1025;

    /// <summary>
    /// Whether to negotiate TLS.
    /// </summary>
    /// <remarks>
    /// False only for a local catcher. A production server sending credentials
    /// in the clear would be handing them to anything on the path, which is why
    /// this is validated at startup rather than trusted.
    /// </remarks>
    public bool UseStartTls { get; init; }

    public string? Username { get; init; }

    public string? Password { get; init; }

    [Required]
    public string FromAddress { get; init; } = "nao-responda@nahora.dev";

    [Required]
    public string FromName { get; init; } = "NaHora";
}
