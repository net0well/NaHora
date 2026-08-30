import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/errors/app_exception.dart';
import '../../../core/network/network_providers.dart';
import '../domain/session.dart';
import 'auth_providers.dart';
import 'auth_state.dart';

/// Owns the session for the whole application.
///
/// Everything that needs to know whether someone is signed in — the router, the
/// dashboard, the punch button — watches this. It holds no `BuildContext`, so
/// the state can be driven in a test without rendering anything.
class AuthController extends Notifier<AuthState> {
  @override
  AuthState build() {
    // The transport layer reports a dead session by bumping this. Listening
    // rather than being called back keeps core unaware that an auth feature
    // exists.
    ref.listen(sessionExpiredProvider, (_, _) => forgetSession());

    // Restoring is asynchronous, and the router must not send anyone anywhere
    // while the answer is unknown: deciding early would bounce a signed-in user
    // to the login screen for the instant it takes to read the token.
    Future.microtask(restore);

    return const AuthChecking();
  }

  /// Reads the stored session, if there is one still valid.
  Future<void> restore() async {
    try {
      final session = await ref.read(authRepositoryProvider).restore();

      state = session == null ? const AuthSignedOut() : AuthSignedIn(session);
    } on AppException {
      // A restore that fails for any reason lands on the login screen. There is
      // nothing useful to tell someone who has not tried to do anything yet.
      state = const AuthSignedOut();
    }
  }

  Future<Session> signIn({
    required String cpf,
    required String password,
    required String companyId,
  }) async {
    final session = await ref
        .read(authRepositoryProvider)
        .signIn(cpf: cpf, password: password, companyId: companyId);

    state = AuthSignedIn(session);

    return session;
  }

  Future<void> signOut() async {
    await ref.read(authRepositoryProvider).signOut();

    state = const AuthSignedOut();
  }

  /// Drops the session without calling the server.
  ///
  /// Used when a refresh has already failed: the session is gone, and asking
  /// the server to end it would be one more request answering 401.
  Future<void> forgetSession() async {
    await ref.read(tokenStorageProvider).clear();

    state = const AuthSignedOut();
  }
}
