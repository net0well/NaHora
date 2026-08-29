using System.Text;
using FaceId.Domain.Authorization;
using FaceId.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace FaceId.Api.Authentication;

public static class AuthenticationServiceRegistration
{
    public static IServiceCollection AddFaceIdAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
                  ?? throw new InvalidOperationException(
                      $"Configuration section '{JwtOptions.SectionName}' is missing.");

        GuardSigningKey(jwt, environment);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    // Every check stays on. Disabling any of them turns a signed
                    // assertion into a value the client controls.
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),

                    // Small but not zero: a few seconds of drift between hosts
                    // would otherwise reject tokens that are perfectly valid.
                    ClockSkew = jwt.ClockSkew,

                    NameClaimType = JwtRegisteredClaimNames.Sub,
                    RoleClaimType = AuthorizationClaims.Role
                };
            });

        var authorization = services.AddAuthorizationBuilder()
            .SetDefaultPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build());

        // One policy per permission, so an endpoint requires a capability
        // rather than a role. Adding a custom role later changes the role to
        // permission mapping and touches no endpoint.
        foreach (var permission in Permissions.All)
        {
            authorization.AddPolicy(permission, policy => policy
                .RequireAuthenticatedUser()
                .RequireClaim(AuthorizationClaims.Permission, permission));
        }

        return services;
    }

    /// <summary>
    /// Refuses to start outside development with the signing key from
    /// .env.example.
    /// </summary>
    /// <remarks>
    /// An application that boots with the published key works perfectly and
    /// lets anyone forge a token for any company. Failing at startup is loud;
    /// the alternative is silent.
    /// </remarks>
    private static void GuardSigningKey(JwtOptions jwt, IHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            return;
        }

        if (string.Equals(jwt.SigningKey, JwtOptions.ExampleSigningKey, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Jwt:SigningKey still holds the example value. Set a real key before starting outside development.");
        }
    }
}
