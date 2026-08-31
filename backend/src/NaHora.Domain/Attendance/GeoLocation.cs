using NaHora.Domain.Errors;
using NaHora.Shared.Results;

namespace NaHora.Domain.Attendance;

/// <summary>
/// Where a punch happened, as the device reported it.
/// </summary>
/// <remarks>
/// A value object because it has no identity of its own and no meaning outside
/// the punch it belongs to.
///
/// <see cref="AccuracyMeters"/> is stored rather than judged. A radius of 85 m
/// inside a warehouse is normal, and refusing it would be refusing a legitimate
/// punch — which Portaria 671, Art. 78 §1º forbids. The number is evidence for
/// whoever reviews, not a gate.
/// </remarks>
public sealed record GeoLocation
{
    private GeoLocation(double latitude, double longitude, double accuracyMeters, DateTimeOffset capturedAt)
    {
        Latitude = latitude;
        Longitude = longitude;
        AccuracyMeters = accuracyMeters;
        CapturedAt = capturedAt;
    }

    public double Latitude { get; }

    public double Longitude { get; }

    /// <summary>Radius the device reported as its confidence, in metres.</summary>
    public double AccuracyMeters { get; }

    /// <summary>
    /// When the fix was taken on the device — not when the punch was recorded.
    /// </summary>
    /// <remarks>
    /// The two differ when a punch waits in the offline queue, and the gap is
    /// worth keeping: a fix from four hours before the punch describes
    /// somewhere the person no longer was.
    /// </remarks>
    public DateTimeOffset CapturedAt { get; }

    /// <summary>
    /// Whether this reading is the very same point as another, digit for digit.
    /// </summary>
    /// <remarks>
    /// Not "nearby". A GPS standing still drifts by metres between readings and
    /// never returns bit-identical coordinates twice, so an exact repeat is a
    /// position being replayed rather than measured.
    ///
    /// Accuracy is deliberately not compared: a fake-location app that varies
    /// the reported accuracy while holding the point still would slip past.
    /// </remarks>
    public bool IsSamePointAs(GeoLocation other)
    {
        ArgumentNullException.ThrowIfNull(other);

        return Latitude == other.Latitude && Longitude == other.Longitude;
    }

    public static Result<GeoLocation> Create(
        double latitude,
        double longitude,
        double accuracyMeters,
        DateTimeOffset capturedAt)
    {
        var errors = new List<Error>();

        if (double.IsNaN(latitude) || latitude is < -90 or > 90)
        {
            errors.Add(PunchErrors.LatitudeOutOfRange());
        }

        if (double.IsNaN(longitude) || longitude is < -180 or > 180)
        {
            errors.Add(PunchErrors.LongitudeOutOfRange());
        }

        // Negative accuracy is not a bad reading, it is a malformed one: no
        // device reports a negative radius.
        if (double.IsNaN(accuracyMeters) || accuracyMeters < 0)
        {
            errors.Add(PunchErrors.AccuracyInvalid());
        }

        return errors.Count > 0
            ? Result.Failure<GeoLocation>(errors)
            : Result.Success(new GeoLocation(latitude, longitude, accuracyMeters, capturedAt));
    }
}
