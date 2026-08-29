using FaceId.Domain.Abstractions;

namespace FaceId.Domain.Users;

/// <summary>
/// A revocable session credential.
/// </summary>
/// <remarks>
/// Its own root rather than a child of <see cref="User"/>: refresh is looked up
/// by token hash before the user is known, and revoking a family touches many
/// rows at once. Loading a user aggregate to answer either would be the wrong
/// shape.
///
/// The token itself is never stored. Only its SHA-256 lives here, so a database
/// dump cannot be replayed as a live session.
/// </remarks>
[Audit(IgnoredProperties = [nameof(TokenHash)])]
public sealed class RefreshToken : AggregateRoot, ITenantOwned
{
    private RefreshToken()
    {
        TokenHash = string.Empty;
    }

    private RefreshToken(
        Guid companyId,
        Guid userId,
        Guid familyId,
        string tokenHash,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt,
        string? createdByIp,
        string? userAgent)
    {
        CompanyId = companyId;
        UserId = userId;
        FamilyId = familyId;
        TokenHash = tokenHash;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
        CreatedByIp = createdByIp;
        UserAgent = userAgent;
    }

    public Guid CompanyId { get; private set; }

    public Guid UserId { get; private set; }

    /// <summary>
    /// Constant across the whole rotation chain of one sign-in. Presenting an
    /// already-rotated token revokes the entire family, because the legitimate
    /// holder and a thief cannot both continue.
    /// </summary>
    public Guid FamilyId { get; private set; }

    /// <summary>SHA-256 of the token. The plaintext is never persisted.</summary>
    public string TokenHash { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    /// <summary>Set when this token was rotated, pointing at its replacement.</summary>
    public Guid? ReplacedByTokenId { get; private set; }

    public string? CreatedByIp { get; private set; }

    public string? UserAgent { get; private set; }

    public bool IsRevoked => RevokedAt is not null;

    /// <summary>Starts a new session chain.</summary>
    public static RefreshToken Issue(
        Guid companyId,
        Guid userId,
        string tokenHash,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt,
        string? createdByIp = null,
        string? userAgent = null) =>
        new(
            companyId,
            userId,
            familyId: Guid.CreateVersion7(),
            tokenHash,
            createdAt,
            expiresAt,
            createdByIp,
            userAgent);

    /// <summary>Continues an existing chain, keeping the family.</summary>
    public RefreshToken Rotate(
        string tokenHash,
        DateTimeOffset now,
        DateTimeOffset expiresAt,
        string? createdByIp = null,
        string? userAgent = null)
    {
        var replacement = new RefreshToken(
            CompanyId,
            UserId,
            FamilyId,
            tokenHash,
            now,
            expiresAt,
            createdByIp,
            userAgent);

        RevokedAt = now;
        ReplacedByTokenId = replacement.Id;

        return replacement;
    }

    public void Revoke(DateTimeOffset now) => RevokedAt ??= now;

    public bool IsActiveAt(DateTimeOffset now) => !IsRevoked && ExpiresAt > now;
}
