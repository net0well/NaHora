namespace FaceId.Features.Authentication.Login;

/// <summary>Credentials presented at sign-in.</summary>
/// <param name="Cpf">The credential. Accepted with or without punctuation.</param>
/// <param name="Password">The plaintext password. Never logged.</param>
/// <param name="CompanyId">
/// Which company to sign in to. The same CPF may belong to more than one
/// company on the same instance, and there is no session yet to infer it from.
/// </param>
public sealed record LoginCommand(string Cpf, string Password, Guid CompanyId)
{
    /// <summary>
    /// Caller address, filled by the endpoint from the connection.
    /// </summary>
    /// <remarks>
    /// Never bound from the request body. A client-supplied address in an audit
    /// trail is worse than no address, because it looks trustworthy.
    /// </remarks>
    public string? IpAddress { get; init; }
}
