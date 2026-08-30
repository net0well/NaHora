using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using NaHora.Domain.Adjustments;
using NaHora.Infrastructure.Configuration;
using NaHora.Shared.DependencyInjection;

namespace NaHora.Infrastructure.Storage;

/// <summary>
/// Keeps justification attachments in MinIO.
/// </summary>
/// <remarks>
/// <para>
/// Named for the technology it adapts rather than for the pattern: swapping it
/// for S3 means a new class with a name that says so.
/// </para>
/// <para>
/// Public so Wolverine's generated handler code can construct it directly.
/// </para>
/// </remarks>
public sealed class MinioAttachmentStorage(IOptions<StorageOptions> options) : IAttachmentStorage, IScopedService
{
    /// <summary>
    /// How long a link stays good.
    /// </summary>
    /// <remarks>
    /// Long enough to open a document, short enough that a URL pasted into a
    /// chat is useless by the time somebody else reads it.
    /// </remarks>
    private static readonly TimeSpan LinkLifetime = TimeSpan.FromMinutes(5);

    public async Task<string> SaveAsync(
        Guid companyId,
        Guid adjustmentId,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var settings = options.Value;

        // Company first, so a key that leaks cannot name an object belonging to
        // anybody else.
        var objectKey = $"{companyId}/{adjustmentId}{ExtensionFor(contentType)}";

        var client = BuildClient(settings);

        await client.PutObjectAsync(
            new PutObjectArgs()
                .WithBucket(settings.AttachmentsBucket)
                .WithObject(objectKey)
                .WithStreamData(content)
                .WithObjectSize(content.Length)
                .WithContentType(contentType),
            cancellationToken);

        return objectKey;
    }

    public async Task<Uri> GetTemporaryUrlAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        var client = BuildClient(settings);

        var url = await client.PresignedGetObjectAsync(
            new PresignedGetObjectArgs()
                .WithBucket(settings.AttachmentsBucket)
                .WithObject(objectKey)
                .WithExpiry((int)LinkLifetime.TotalSeconds));

        return new Uri(url);
    }

    private static IMinioClient BuildClient(StorageOptions settings) =>
        new MinioClient()
            .WithEndpoint(settings.Endpoint)
            .WithCredentials(settings.AccessKey, settings.SecretKey)
            .WithSSL(settings.UseSsl)
            .Build();

    /// <summary>
    /// Keeps the extension the content type implies, so a downloaded file opens
    /// in the right application instead of asking the person what it is.
    /// </summary>
    private static string ExtensionFor(string contentType) => contentType switch
    {
        "image/jpeg" => ".jpg",
        "image/png" => ".png",
        "image/heic" => ".heic",
        "application/pdf" => ".pdf",
        _ => string.Empty
    };
}
