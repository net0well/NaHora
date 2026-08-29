namespace FaceId.Infrastructure.Configuration;

/// <summary>
/// RabbitMQ connection for the outbox transport.
/// </summary>
/// <remarks>
/// <see cref="Enabled"/> exists so the broker is not a hard startup dependency
/// everywhere. Integration tests and any environment without RabbitMQ run with
/// the outbox writing locally and handlers invoked in process, which keeps the
/// transactional guarantee while dropping the transport.
/// </remarks>
public sealed class MessagingOptions
{
    public const string SectionName = "Messaging";

    public bool Enabled { get; init; }

    public string Host { get; init; } = "localhost";

    public int Port { get; init; } = 5672;

    public string Username { get; init; } = "guest";

    public string Password { get; init; } = "guest";

    public string VirtualHost { get; init; } = "/";

    public string Exchange { get; init; } = "faceid.events";

    /// <summary>
    /// Queue this instance listens on. Bound to the exchange, so a message
    /// published here comes back to the consumer instead of leaving and
    /// vanishing.
    /// </summary>
    public string Queue { get; init; } = "faceid.events.queue";
}
