using System.ComponentModel.DataAnnotations;

namespace NaHora.Infrastructure.Configuration;

/// <summary>
/// Object storage for the files the system has to keep.
/// </summary>
/// <remarks>
/// One bucket per retention rule, not per feature. An attachment follows the
/// punch it justifies and is kept for years; an export is disposable and exists
/// only until someone downloads it. Mixing the two would force the stricter
/// rule onto both, and the stricter rule here means keeping megabytes of
/// throwaway spreadsheets for as long as a medical certificate.
/// </remarks>
public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    [Required(AllowEmptyStrings = false)]
    public string Endpoint { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string AccessKey { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string SecretKey { get; init; } = string.Empty;

    public bool UseSsl { get; init; }

    /// <summary>
    /// Justification attachments — medical certificates, declarations, receipts.
    /// </summary>
    /// <remarks>
    /// Personal documents. Never a public bucket: access is by short-lived
    /// signed URL, and the object key is prefixed by company so tenant
    /// isolation holds in storage too, not only in the database.
    /// </remarks>
    public string AttachmentsBucket { get; init; } = "nahora-attachments";

    /// <summary>Generated reports. Disposable, with a short lifetime.</summary>
    public string ExportsBucket { get; init; } = "nahora-exports";

    /// <summary>AFD, AEJ and signed compliance artefacts.</summary>
    public string ComplianceBucket { get; init; } = "nahora-compliance";
}
