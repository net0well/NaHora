using NaHora.Shared.Results;

namespace NaHora.Domain.Errors;

/// <summary>
/// Failures of the punch aggregate.
/// </summary>
/// <remarks>
/// Deliberately short. Almost nothing about a punch is refusable — Portaria
/// 671, Art. 78 §1º forbids the registrar from restricting the record, so bad
/// accuracy, a mocked location and a wrong device clock are all classified
/// rather than rejected. What remains here is malformed input and the two cases
/// that are about the caller, not the punch.
/// </remarks>
public static class PunchErrors
{
    public static Error LocationRequired() => Error.Validation(
        "LOCATION_REQUIRED",
        "A punch must carry the location it was made at.",
        "location");

    public static Error LatitudeOutOfRange() => Error.Validation(
        "LATITUDE_OUT_OF_RANGE",
        "Latitude must be between -90 and 90.",
        "location.latitude");

    public static Error LongitudeOutOfRange() => Error.Validation(
        "LONGITUDE_OUT_OF_RANGE",
        "Longitude must be between -180 and 180.",
        "location.longitude");

    public static Error AccuracyInvalid() => Error.Validation(
        "ACCURACY_INVALID",
        "Accuracy must be a non-negative number of metres.",
        "location.accuracyMeters");

    public static Error ClientIdRequired() => Error.Validation(
        "CLIENT_ID_REQUIRED",
        "A punch must carry the identifier the device generated for it.",
        "clientId");

    public static Error NotFound() => Error.NotFound(
        "PUNCH_NOT_FOUND",
        "Punch not found.");

    /// <summary>
    /// The employee has no employment record, so there is no working day to
    /// punch against.
    /// </summary>
    /// <remarks>
    /// The one refusal that is not about the punch: it is about the caller not
    /// being someone who punches at all, and it is decided before the act.
    /// </remarks>
    public static Error EmployeeRequired() => Error.Forbidden(
        "EMPLOYEE_RECORD_REQUIRED",
        "Only an employee with an active employment record can punch.");
}
