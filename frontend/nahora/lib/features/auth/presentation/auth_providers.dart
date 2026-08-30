import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network_providers.dart';
import '../data/auth_repository.dart';
import 'auth_controller.dart';
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
