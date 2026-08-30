import 'authenticated_user.dart';

/// An established session.
class Session {
  const Session({
    required this.user,
    required this.pendingActions,
    required this.expiresAt,
  });

  final AuthenticatedUser user;

  /// What the user still has to complete before the app is fully usable.
  ///
  /// Sign-in succeeds with actions outstanding — without a token there would be
  /// no credential to call the endpoints that resolve them.
  final List<String> pendingActions;

  final DateTime expiresAt;

  bool get mustChangePassword => pendingActions.contains('ChangePassword');

  bool get mustGrantLocationPermission =>
      pendingActions.contains('GrantLocationPermission');
}
