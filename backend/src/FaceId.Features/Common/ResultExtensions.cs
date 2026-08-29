using FaceId.Shared.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace FaceId.Features.Common;

/// <summary>
/// Translates a <see cref="Result"/> into an HTTP response.
/// </summary>
/// <remarks>
/// The single place where a business outcome becomes a status code. Endpoints
/// never build a problem response by hand, so the contract stays uniform across
/// every slice.
///
/// Unexpected failures never reach here: they surface as exceptions and the
/// global handler turns them into a generic 500. Either way the client receives
/// problem+json and never a stack trace.
/// </remarks>
public static class ResultExtensions
{
    /// <summary>Maps a successful result to 200, a failure to its status.</summary>
    public static IResult ToOk<TValue>(this Result<TValue> result) =>
        result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();

    /// <summary>Maps a successful result to 201 with a Location header.</summary>
    public static IResult ToCreated<TValue>(this Result<TValue> result, string location) =>
        result.IsSuccess ? TypedResults.Created(location, result.Value) : result.ToProblem();

    /// <summary>
    /// Maps a successful result to 202, for work that was accepted but is not
    /// finished — an async export, a company awaiting email confirmation.
    /// </summary>
    public static IResult ToAccepted<TValue>(this Result<TValue> result, string? location = null) =>
        result.IsSuccess ? TypedResults.Accepted(location, result.Value) : result.ToProblem();

    /// <summary>Maps a successful result with no payload to 204.</summary>
    public static IResult ToNoContent(this Result result) =>
        result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();

    public static IResult ToProblem(this Result result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsSuccess)
        {
            throw new InvalidOperationException("A successful result cannot be turned into a problem response.");
        }

        var primary = result.FirstError!;
        var status = StatusCodeFor(primary.Type);

        var extensions = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            // Stable identifier the Flutter app translates on. The message may
            // be reworded freely; this may not.
            ["code"] = primary.Code
        };

        // Every error, not just the first: a form should surface all of its
        // problems in one pass.
        if (result.Errors.Count > 1 || primary.Field is not null)
        {
            extensions["errors"] = result.Errors
                .Select(error => new { code = error.Code, message = error.Message, field = error.Field })
                .ToArray();
        }

        return TypedResults.Problem(
            title: TitleFor(primary.Type),
            detail: primary.Message,
            statusCode: status,
            extensions: extensions);
    }

    private static int StatusCodeFor(ErrorType type) => type switch
    {
        // 422, not 400: the request was well formed and understood, and failed
        // a business rule. 400 is for a payload the server could not parse.
        ErrorType.Validation => StatusCodes.Status422UnprocessableEntity,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Unavailable => StatusCodes.Status503ServiceUnavailable,
        _ => StatusCodes.Status400BadRequest
    };

    private static string TitleFor(ErrorType type) => type switch
    {
        ErrorType.Validation => "Dados inválidos",
        ErrorType.NotFound => "Registro não encontrado",
        ErrorType.Conflict => "Conflito com o estado atual",
        ErrorType.Forbidden => "Acesso negado",
        ErrorType.Unauthorized => "Não autenticado",
        ErrorType.Unavailable => "Serviço indisponível",
        _ => "Não foi possível concluir a operação"
    };
}
