namespace NaHora.Domain.Attendance;

/// <summary>
/// How much the record can be relied on, decided from evidence and never used
/// to refuse a punch.
/// </summary>
/// <remarks>
/// This is the classification that replaced the biometric confidence score. It
/// answers "should a human look at this?", never "should this be recorded?" —
/// Portaria 671, Art. 78 §1º settles the second question for every value here.
/// </remarks>
public enum PunchTrust
{
    /// <summary>Live, precise, clock in agreement.</summary>
    Trusted = 1,

    /// <summary>Poor accuracy or an offline estimate. Normal, worth noting.</summary>
    Degraded = 2,

    /// <summary>Mocked location or a clock far out of step. Needs a human.</summary>
    NeedsReview = 3
}
