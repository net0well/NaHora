using System.ComponentModel.DataAnnotations;

namespace NaHora.Infrastructure.Configuration;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    [Required(AllowEmptyStrings = false)]
    public string ConnectionString { get; init; } = string.Empty;

    /// <summary>
    /// Applies pending migrations on startup. Development only: in production a
    /// migration runs as its own deployment step, not as a side effect of a pod
    /// starting.
    /// </summary>
    public bool AutoMigrate { get; init; }

    [Range(1, 300)]
    public int CommandTimeoutSeconds { get; init; } = 30;
}
