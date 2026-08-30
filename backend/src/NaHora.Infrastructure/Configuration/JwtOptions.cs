using System.ComponentModel.DataAnnotations;

namespace NaHora.Infrastructure.Configuration;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// Value shipped in .env.example. Rejected outside development so a build
    /// cannot reach production signing tokens anyone could forge.
    /// </summary>
    public const string ExampleSigningKey = "CHANGE_ME_generate_at_least_32_bytes";

    [Required(AllowEmptyStrings = false)]
    public string Issuer { get; init; } = "nahora";

    [Required(AllowEmptyStrings = false)]
    public string Audience { get; init; } = "nahora-app";

    /// <summary>
    /// HMAC key. At least 32 bytes, because HS256 truncating a shorter key
    /// silently weakens every token signed with it.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    [MinLength(32)]
    public string SigningKey { get; init; } = string.Empty;

    /// <summary>
    /// Short on purpose. The access token cannot be revoked, so its lifetime is
    /// the window in which a deactivated user still gets through. Revocation
    /// lives on the refresh token instead.
    /// </summary>
    [Range(1, 60)]
    public int AccessTokenMinutes { get; init; } = 15;

    [Range(1, 90)]
    public int RefreshTokenDays { get; init; } = 14;

    /// <summary>Tolerance for clock drift between the issuer and the validator.</summary>
    [Range(0, 300)]
    public int ClockSkewSeconds { get; init; } = 30;

    public TimeSpan AccessTokenLifetime => TimeSpan.FromMinutes(AccessTokenMinutes);

    public TimeSpan RefreshTokenLifetime => TimeSpan.FromDays(RefreshTokenDays);

    public TimeSpan ClockSkew => TimeSpan.FromSeconds(ClockSkewSeconds);
}
