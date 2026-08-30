using Microsoft.Extensions.Logging;

namespace NaHora.Infrastructure.Registry;

internal static partial class RegistryLog
{
    [LoggerMessage(
        EventId = 3001,
        Level = LogLevel.Warning,
        Message = "Tax registry lookup returned {StatusCode}; proceeding unverified")]
    public static partial void LookupUnavailable(ILogger logger, int statusCode);

    [LoggerMessage(
        EventId = 3002,
        Level = LogLevel.Warning,
        Message = "Tax registry lookup could not be completed; proceeding unverified")]
    public static partial void LookupFailed(ILogger logger, Exception exception);
}
