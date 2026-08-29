namespace FaceId.Features.Authentication.Login;

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
    public const string RegisterFaceProfile = "RegisterFaceProfile";
}
