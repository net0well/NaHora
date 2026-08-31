namespace NaHora.Domain.Adjustments;

/// <summary>
/// Keeps the documents that back a correction — a medical certificate, a
/// declaration, a receipt.
/// </summary>
/// <remarks>
/// A port so the domain can say "keep this and give me back a key" without
/// knowing whether the bytes land in MinIO, S3 or a folder.
/// </remarks>
public interface IAttachmentStorage
{
    /// <summary>
    /// Stores the file and returns the key that identifies it.
    /// </summary>
    /// <remarks>
    /// The key is prefixed by company, so tenant isolation holds in storage as
    /// well as in the database — a leaked key from one company cannot name an
    /// object in another.
    /// </remarks>
    Task<string> SaveAsync(
        Guid companyId,
        Guid adjustmentId,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// A link that works for a few minutes and then does not.
    /// </summary>
    /// <remarks>
    /// Never a public object. These are somebody's medical records; a URL that
    /// keeps working is a URL that gets forwarded, and the bucket is not
    /// browsable by design.
    /// </remarks>
    Task<Uri> GetTemporaryUrlAsync(string objectKey, CancellationToken cancellationToken = default);
}
