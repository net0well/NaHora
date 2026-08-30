namespace NaHora.Features.Authentication.Login;

/// <summary>
/// What the user still has to complete before the rest of the application
/// opens up.
/// </summary>
/// <remarks>
/// Sign-in succeeds even with actions outstanding. It has to: without a token
/// there would be no credential to call the endpoints that resolve them.
/// </remarks>
public static class PendingAction
{
    public const string ChangePassword = "ChangePassword";

    /// <summary>
    /// The app has no location permission yet, so it cannot punch.
    /// </summary>
    /// <remarks>
    /// Reported by the server as an outstanding action so the client shows the
    /// explanation screen before the system dialog. A permission prompt with no
    /// context is denied by reflex, and recovering from that means walking the
    /// employee through Android settings.
    /// </remarks>
    public const string GrantLocationPermission = "GrantLocationPermission";
}
