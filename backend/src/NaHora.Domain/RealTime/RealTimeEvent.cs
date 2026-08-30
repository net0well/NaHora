namespace NaHora.Domain.RealTime;

/// <summary>
/// Something that happened which connected observers of a company should see.
/// </summary>
/// <remarks>
/// This is both the message that travels between instances and the payload
/// written to the wire, deliberately: a second type would only be a copy of
/// this one with a mapper between them.
///
/// <see cref="Data"/> is a string map rather than a typed payload per event
/// because the channel is generic and the destination is a screen. A typed
/// hierarchy would force polymorphic serialisation on the bus and in the SSE
/// body to gain a precision the dashboard does not use — it renders text.
/// </remarks>
/// <param name="Id">
/// Time-ordered, so a client resuming with <c>Last-Event-ID</c> has a
/// meaningful position rather than an opaque token.
/// </param>
/// <param name="CompanyId">
/// The only audience. Filled from the producer's own tenant, and matched
/// against the observer's tenant taken from the token — never from the request.
/// </param>
public sealed record RealTimeEvent(
    string Id,
    string Type,
    Guid CompanyId,
    DateTimeOffset OccurredAt,
    IReadOnlyDictionary<string, string> Data)
{
    public static RealTimeEvent Create(
        string type,
        Guid companyId,
        DateTimeOffset occurredAt,
        IReadOnlyDictionary<string, string>? data = null) =>
        new(Guid.CreateVersion7(occurredAt).ToString("n"), type, companyId, occurredAt, data ?? EmptyData);

    private static readonly IReadOnlyDictionary<string, string> EmptyData =
        new Dictionary<string, string>();
}
