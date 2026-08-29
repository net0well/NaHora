namespace FaceId.Features.Authentication.Auditing;

/// <summary>
/// Log category for the authentication audit trail.
/// </summary>
/// <remarks>
/// An empty type because ILogger takes its category from a type argument, and a
/// static handler offers none. Naming the category explicitly lets this trail
/// be tuned independently of the rest of the application's logging.
/// </remarks>
public sealed class AuthenticationAuditHandlerCategory;
