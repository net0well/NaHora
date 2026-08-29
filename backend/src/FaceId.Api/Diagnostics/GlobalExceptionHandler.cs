using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace FaceId.Api.Diagnostics;

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

        logger.LogError(
            exception,
            "Unhandled exception on {Method} {Path}",
            httpContext.Request.Method,
            httpContext.Request.Path);

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Erro inesperado",
            Detail = "Não foi possível concluir a operação. Tente novamente.",
            Instance = httpContext.Request.Path
        };

        problem.Extensions["code"] = "INTERNAL_ERROR";
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken).ConfigureAwait(false);

        return true;
    }
}
