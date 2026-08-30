using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using NaHora.Domain.Authorization;
using NaHora.Domain.Users;
using NaHora.Infrastructure.Configuration;
using NaHora.Shared.DependencyInjection;

namespace NaHora.Infrastructure.Identity;

/// <summary>
/// Issues access and refresh tokens.
/// </summary>
/// <remarks>
/// Uses <see cref="JsonWebTokenHandler"/>, the maintained span-based handler
/// that ASP.NET Core itself validates with, rather than the legacy
/// <c>JwtSecurityTokenHandler</c>.
///
/// Public so Wolverine's generated handler code can construct it directly; an
/// internal adapter would force service location, which the runtime rejects.
/// </remarks>
public sealed class JwtAccessTokenService(IOptions<JwtOptions> options, TimeProvider clock)
    : IAccessTokenService, ISingletonService
{
    private const int RefreshTokenBytes = 32;

    private static readonly JsonWebTokenHandler TokenHandler = new();

    private readonly JwtOptions _options = options.Value;

    public IssuedAccessToken IssueAccessToken(User user, Guid? employeeId)
    {
        ArgumentNullException.ThrowIfNull(user);

        var now = clock.GetUtcNow();
        var expiresAt = now.Add(_options.AccessTokenLifetime);

        var claims = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),
            [JwtRegisteredClaimNames.Jti] = Guid.CreateVersion7().ToString(),
            [AuthorizationClaims.CompanyId] = user.CompanyId.ToString(),
            [AuthorizationClaims.Role] = user.Role.ToString(),

            // Permissions travel in the token so authorisation costs no query.
            // The trade-off is that a role change only takes effect on the next
            // refresh, which the short access lifetime keeps small.
            [AuthorizationClaims.Permission] = RolePermissions.For(user.Role).ToArray()
        };

        if (employeeId is { } id)
        {
            claims[AuthorizationClaims.EmployeeId] = id.ToString();
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            Claims = claims,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
                SecurityAlgorithms.HmacSha256)
        };

        return new IssuedAccessToken(TokenHandler.CreateToken(descriptor), expiresAt);
    }

    public IssuedRefreshToken IssueRefreshToken()
    {
        // Opaque and unguessable. It carries no claims: its only job is to be
        // looked up and revoked, and anything encoded in it would be one more
        // thing an attacker could read.
        var token = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(RefreshTokenBytes));

        return new IssuedRefreshToken(
            token,
            HashRefreshToken(token),
            clock.GetUtcNow().Add(_options.RefreshTokenLifetime));
    }

    public string HashRefreshToken(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        // SHA-256 without a work factor is right here, unlike for a password:
        // the token is 32 random bytes, so there is no dictionary to attack and
        // no reason to pay for slow hashing on every refresh.
        return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
