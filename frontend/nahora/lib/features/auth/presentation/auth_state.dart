import '../domain/session.dart';

/// Where the application stands with respect to a session.
sealed class AuthState {
  const AuthState();
}

/// Start-up: a stored token may or may not still be good, and the router must
/// not decide anything yet.
class AuthChecking extends AuthState {
  const AuthChecking();
}

class AuthSignedOut extends AuthState {
  const AuthSignedOut();
}

class AuthSignedIn extends AuthState {
  const AuthSignedIn(this.session);

  final Session session;
}
