namespace NaHora.Api.Diagnostics;

/// <summary>
/// Source-generated log messages for the startup migration step.
/// </summary>
internal static partial class DatabaseMigratorLog
{
    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Information,
        Message = "Applying {Count} pending migration(s): {Migrations}")]
    public static partial void ApplyingMigrations(ILogger logger, int count, string[] migrations);

    [LoggerMessage(
        EventId = 2002,
        Level = LogLevel.Warning,
        Message = "Database:AutoMigrate is enabled outside development and was ignored. "
                  + "Apply migrations as a separate deployment step.")]
    public static partial void AutoMigrateIgnored(ILogger logger);
}
