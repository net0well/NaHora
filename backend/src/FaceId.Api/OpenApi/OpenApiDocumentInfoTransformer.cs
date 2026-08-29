using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace FaceId.Api.OpenApi;

/// <summary>
/// Fills in the document metadata that the generator cannot infer from code.
/// </summary>
internal sealed class OpenApiDocumentInfoTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);

        document.Info = new OpenApiInfo
        {
            Title = "FaceId API",
            Version = "v1",
            Description =
                "Controle de ponto com reconhecimento facial, anti-fraude e conformidade "
                + "com a Portaria MTP 671/2021 (REP-P).\n\n"
                + "Erros seguem RFC 9457 (`application/problem+json`) e sempre trazem um campo "
                + "`code` estável — é nele que o cliente decide a mensagem, não no texto."
        };

        return Task.CompletedTask;
    }
}
