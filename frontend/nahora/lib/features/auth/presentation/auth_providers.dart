import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network_providers.dart';
import '../data/auth_repository.dart';
import 'auth_controller.dart';
import '../domain/session.dart';
import 'auth_state.dart';

export '../../../core/network/network_providers.dart' show tokenStorageProvider;

final authRepositoryProvider = Provider<AuthRepository>((ref) {
  return AuthRepository(
    client: ref.watch(apiClientProvider),
    storage: ref.watch(tokenStorageProvider),
  );
});

final authControllerProvider = NotifierProvider<AuthController, AuthState>(
  AuthController.new,
);

/// The established session, or null when nobody is signed in.
///
/// Saves every screen from unwrapping the auth state itself, and keeps the
/// unwrapping in one place if the state ever gains a case.
final currentSessionProvider = Provider<Session?>((ref) {
  final auth = ref.watch(authControllerProvider);

  return auth is AuthSignedIn ? auth.session : null;
});
