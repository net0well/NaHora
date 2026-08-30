namespace NaHora.Domain.Users;

/// <summary>
/// Lifecycle of an authenticable identity.
/// </summary>
public enum UserStatus
{
    /// <summary>
    /// Authenticates successfully, but onboarding is incomplete: the password
    /// was set by someone else, or the face profile is missing. Sign-in has to
    /// succeed, otherwise there would be no credential to complete onboarding
    /// with.
    /// </summary>
    PendingFirstAccess = 0,

    Active = 1,

    /// <summary>Cannot authenticate. Records remain intact.</summary>
    Inactive = 2
}
