using System.ComponentModel.DataAnnotations;

namespace FaceId.Infrastructure.Configuration;

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

    public string EvidenceBucket { get; init; } = "faceid-security-evidence";

    public string ExportsBucket { get; init; } = "faceid-exports";

    public string ComplianceBucket { get; init; } = "faceid-compliance";
}
