using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace NaHora.Api.Observability;

/// <summary>
/// Names and instruments for the application's own telemetry.
/// </summary>
/// <remarks>
/// No instrument ever carries a coordinate, as a value or as a label. A metric
/// label is a cardinality problem and a privacy problem at the same time:
/// latitude and longitude are unique per punch and would identify where a named
/// employee was at a given minute. What is tagged is the classification —
/// accuracy band, origin, whether the location was mocked — which is low
/// cardinality and tells the operator what they actually need.
/// </remarks>
public static class NaHoraTelemetry
{
    public const string ServiceName = "NaHora.Api";

    /// <summary>Spans produced by application code, as opposed to instrumentation.</summary>
    public static readonly ActivitySource ActivitySource = new(ServiceName);

    public static readonly Meter Meter = new(ServiceName);

    /// <summary>
    /// Punches recorded, tagged by origin and by whether the location looked
    /// trustworthy. Never by coordinate.
    /// </summary>
    public static readonly Counter<long> AttendanceRecorded =
        Meter.CreateCounter<long>(
            "nahora.attendance.recorded",
            unit: "{record}",
            description: "Punches recorded, tagged by origin and location trust.");

    public static readonly Counter<long> SecurityEvents =
        Meter.CreateCounter<long>(
            "nahora.security.events",
            unit: "{event}",
            description: "Security events created, tagged by type and severity.");

    public static readonly Counter<long> AuthenticationAttempts =
        Meter.CreateCounter<long>(
            "nahora.auth.attempts",
            unit: "{attempt}",
            description: "Sign-in attempts, tagged by outcome.");

    /// <summary>
    /// How long a punch waited in the device queue before reaching the server.
    /// </summary>
    /// <remarks>
    /// The number that says whether offline mode is working. A distribution
    /// creeping upwards means punches are sitting on phones — which is data at
    /// risk of being lost with the device, not merely a latency figure.
    /// </remarks>
    public static readonly Histogram<double> OfflineQueueDelay =
        Meter.CreateHistogram<double>(
            "nahora.attendance.offline_queue_delay",
            unit: "s",
            description: "Seconds between a punch occurring and reaching the server.");
}
