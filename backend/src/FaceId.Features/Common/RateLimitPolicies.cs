namespace FaceId.Features.Common;

/// <summary>
/// Names of the rate limiting policies.
/// </summary>
/// <remarks>
/// Declared here, next to the endpoints that apply them, rather than in the
/// composition root that configures them: a slice must not reference the Api
/// project. An endpoint never spells a policy as a literal, because a typo in a
/// literal fails open — the endpoint simply gets no limit at all.
/// </summary>
public static class RateLimitPolicies
{
    /// <summary>
    /// Sign-in. Guards against password spraying, which the per-account lockout
    /// cannot see: a thousand CPFs tried once each never reaches any account's
    /// attempt limit.
    /// </summary>
    public const string Login = "auth-login";

    /// <summary>Self sign-up, against mass company creation.</summary>
    public const string CompanyRegistration = "company-registration";

    /// <summary>Public CNPJ lookup, against using this endpoint to enumerate the tax registry.</summary>
    public const string CnpjLookup = "cnpj-lookup";

    /// <summary>Export, which is both expensive and a way to extract personal data.</summary>
    public const string Export = "export";

    /// <summary>
    /// The live event stream, limited by concurrency rather than by rate.
    /// </summary>
    /// <remarks>
    /// A subscription is one request that stays open, so counting requests per
    /// minute measures nothing here — what is scarce is the connection itself.
    /// Without a cap, a client that reconnects without closing the previous
    /// stream holds a socket per attempt until the server runs out.
    /// </remarks>
    public const string EventStream = "event-stream";
}
