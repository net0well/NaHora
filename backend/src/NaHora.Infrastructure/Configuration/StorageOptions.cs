using System.ComponentModel.DataAnnotations;

namespace NaHora.Infrastructure.Configuration;

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

    public string EvidenceBucket { get; init; } = "nahora-security-evidence";

    public string ExportsBucket { get; init; } = "nahora-exports";

    public string ComplianceBucket { get; init; } = "nahora-compliance";
}
