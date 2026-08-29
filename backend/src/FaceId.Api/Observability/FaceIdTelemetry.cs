using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace FaceId.Api.Observability;

/// <summary>
/// Names and instruments for the application's own telemetry.
/// </summary>
/// <remarks>
/// The biometric pipeline gets a span per stage, which is what answers "which
/// stage consumed the 1200 ms budget" without anyone instrumenting by hand.
///
/// No instrument ever carries a score, as a value or as a label. Only the
/// resulting classification, which is low cardinality and is not sensitive
/// data.
/// </remarks>
public static class FaceIdTelemetry
{
    public const string ServiceName = "FaceId.Api";

    /// <summary>Spans produced by application code, as opposed to instrumentation.</summary>
    public static readonly ActivitySource ActivitySource = new(ServiceName);

    public static readonly Meter Meter = new(ServiceName);

    /// <summary>Stage names used as span names inside the biometric pipeline.</summary>
    public static class BiometricStages
    {
        public const string Detection = "biometrics.detection";
        public const string Quality = "biometrics.quality";
        public const string Alignment = "biometrics.alignment";
        public const string Liveness = "biometrics.liveness";
        public const string Embedding = "biometrics.embedding";
        public const string Similarity = "biometrics.similarity";
        public const string Decision = "biometrics.decision";
    }

    /// <summary>
    /// Attendance records by confidence. Deliberately the classification and
    /// not the score behind it.
    /// </summary>
    public static readonly Counter<long> AttendanceRecorded =
        Meter.CreateCounter<long>(
            "faceid.attendance.recorded",
            unit: "{record}",
            description: "Attendance records created, tagged by confidence.");

    public static readonly Counter<long> SecurityEvents =
        Meter.CreateCounter<long>(
            "faceid.security.events",
            unit: "{event}",
            description: "Security events created, tagged by type and severity.");

    public static readonly Counter<long> AuthenticationAttempts =
        Meter.CreateCounter<long>(
            "faceid.auth.attempts",
            unit: "{attempt}",
            description: "Sign-in attempts, tagged by outcome.");

    /// <summary>
    /// Latency of one biometric stage. The histogram is what turns the p95
    /// budget into something observable rather than aspirational.
    /// </summary>
    public static readonly Histogram<double> BiometricStageDuration =
        Meter.CreateHistogram<double>(
            "faceid.biometrics.stage.duration",
            unit: "ms",
            description: "Duration of a single biometric pipeline stage.");
}
