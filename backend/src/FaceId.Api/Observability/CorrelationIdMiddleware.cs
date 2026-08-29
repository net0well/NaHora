using System.Diagnostics;
using Serilog.Context;

namespace FaceId.Api.Observability;

/// <summary>
/// Gives every request a correlation identifier and pushes it onto the log
/// context.
/// </summary>
/// <remarks>
/// Accepts an inbound <c>X-Correlation-Id</c> so a client, or the Flutter app
/// retrying a punch, can tie its own attempts together. Falls back to the trace
/// identifier, which keeps logs and traces pointing at the same thing.
///
/// The value is echoed back on the response, so a support conversation can
/// start from what the user actually saw.
/// </remarks>
internal sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string HeaderName = "X-Correlation-Id";

    private const int MaxLength = 64;

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var correlationId = ResolveCorrelationId(context);

        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            Activity.Current?.SetTag("faceid.correlation_id", correlationId);

            await next(context);
        }
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out var provided))
        {
            var candidate = provided.ToString();

            // Bounded and sanitised: the value reaches logs and a response
            // header, and an unbounded client-supplied string in either is a
            // way to inject noise, or worse.
            if (!string.IsNullOrWhiteSpace(candidate) && candidate.Length <= MaxLength && IsSafe(candidate))
            {
                return candidate;
            }
        }

        return Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
    }

    private static bool IsSafe(string value)
    {
        foreach (var character in value)
        {
            if (!char.IsAsciiLetterOrDigit(character) && character is not ('-' or '_' or '.' or ':'))
            {
                return false;
            }
        }

        return true;
    }
}
