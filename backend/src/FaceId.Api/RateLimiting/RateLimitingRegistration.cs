using System.Globalization;
using System.Threading.RateLimiting;
using FaceId.Features.Common;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.JsonWebTokens;

namespace FaceId.Api.RateLimiting;

public static class RateLimitingRegistration
{
    public static IServiceCollection AddFaceIdRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = configuration.GetSection(RateLimitOptions.SectionName).Get<RateLimitOptions>()
                      ?? new RateLimitOptions();

        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Rejection speaks the same RFC 9457 dialect as every other error,
            // so a client has one shape to handle rather than two.
            limiter.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                }

                context.HttpContext.Response.ContentType = "application/problem+json";

                await context.HttpContext.Response.WriteAsJsonAsync(
                    new
                    {
                        type = "https://faceid.app/errors/rate-limited",
                        title = "Muitas requisições",
                        status = StatusCodes.Status429TooManyRequests,
                        code = "RATE_LIMITED",
                        detail = "Você fez muitas requisições. Aguarde um instante e tente novamente.",
                        instance = context.HttpContext.Request.Path.Value
                    },
                    cancellationToken);
            };

            // Safety net over everything, partitioned by user when there is one
            // so a shared office IP does not throttle a whole company.
            limiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: ResolveUserOrIp(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = options.GlobalPerMinute,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));

            AddFixedWindow(limiter, RateLimitPolicies.Login, options.LoginPerMinute, TimeSpan.FromMinutes(1), ResolveIp);
            AddFixedWindow(limiter, RateLimitPolicies.CompanyRegistration, options.CompanyRegistrationPerHour, TimeSpan.FromHours(1), ResolveIp);
            AddFixedWindow(limiter, RateLimitPolicies.CnpjLookup, options.CnpjLookupPerMinute, TimeSpan.FromMinutes(1), ResolveIp);
            AddFixedWindow(limiter, RateLimitPolicies.Export, options.ExportPerHour, TimeSpan.FromHours(1), ResolveUserOrIp);
        });

        return services;
    }

    private static void AddFixedWindow(
        RateLimiterOptions limiter,
        string policyName,
        int permitLimit,
        TimeSpan window,
        Func<HttpContext, string> partitionKey) =>
        limiter.AddPolicy(policyName, context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: partitionKey(context),
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = window,

                    // No queue. Holding a rejected request only moves the
                    // pressure into the server's memory.
                    QueueLimit = 0
                }));

    private static string ResolveIp(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    /// <summary>
    /// Partitions by the authenticated user, falling back to the address.
    /// </summary>
    /// <remarks>
    /// Partitioning an authenticated route by IP would make one office share a
    /// single budget, since everyone leaves through the same NAT address.
    /// </remarks>
    private static string ResolveUserOrIp(HttpContext context)
    {
        var subject = context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        return string.IsNullOrWhiteSpace(subject) ? $"ip:{ResolveIp(context)}" : $"user:{subject}";
    }
}
