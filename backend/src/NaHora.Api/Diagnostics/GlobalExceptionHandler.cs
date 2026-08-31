using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace NaHora.Api.Diagnostics;

/// <summary>
/// Converts unhandled exceptions into RFC 9457 problem responses.
/// </summary>
/// <remarks>
/// The response never carries the exception message or stack trace. The detail
/// the client receives is a stable error code plus a message safe to show a
/// person; the diagnostics go to the log, correlated by trace identifier.
/// </remarks>
internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        // A body the framework could not read is the caller's mistake, not
        // ours. Reported as one it would be a 500 in the metrics and an Error
        // in the log for every malformed request a client cares to send.
        var isMalformedRequest = exception is BadHttpRequestException;

        if (isMalformedRequest)
        {
            logger.LogWarning(
                exception,
                "Malformed request on {Method} {Path}",
                httpContext.Request.Method,
                httpContext.Request.Path);
        }
        else
        {
            logger.LogError(
                exception,
                "Unhandled exception on {Method} {Path}",
                httpContext.Request.Method,
                httpContext.Request.Path);
        }

        var status = isMalformedRequest
            ? StatusCodes.Status400BadRequest
            : StatusCodes.Status500InternalServerError;

        var problem = new ProblemDetails
        {
            Status = status,
            Title = isMalformedRequest ? "Requisição inválida" : "Erro inesperado",
            Detail = isMalformedRequest
                ? "Não foi possível ler a requisição."
                : "Não foi possível concluir a operação. Tente novamente.",
            Instance = httpContext.Request.Path
        };

        // Never the exception message: it names the parameter and the byte
        // offset the parser stopped at, which tells a caller more about the
        // shape of the contract than the contract does.
        problem.Extensions["code"] = isMalformedRequest
            ? "MALFORMED_REQUEST"
            : "INTERNAL_ERROR";
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken).ConfigureAwait(false);

        return true;
    }
}
