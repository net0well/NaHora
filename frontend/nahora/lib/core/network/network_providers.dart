import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../storage/secure_token_storage.dart';
import 'api_client.dart';

/// The platform credential vault.
final tokenStorageProvider = Provider<TokenStorage>(
  (ref) => SecureTokenStorage(),
);

/// Raised when a refresh fails and the session is gone for good.
///
/// A counter rather than a callback so the transport layer can report upwards
/// without knowing that an auth feature exists. Whoever cares — today the
/// session controller, tomorrow anything else — listens.
final sessionExpiredProvider = StateProvider<int>((ref) => 0);

/// The HTTP client every repository uses.
///
/// Built once and shared, because the interceptor serialises a single in-flight
/// refresh. One client per repository would mean one refresh per repository,
/// and concurrent refreshes rotate the token out from under each other — which
/// the server reads as theft and answers by revoking the whole family.
final apiClientProvider = Provider<Dio>((ref) {
  return ApiClient.create(
    storage: ref.watch(tokenStorageProvider),
    onSessionLost: () async {
      await ref.read(tokenStorageProvider).clear();
      ref.read(sessionExpiredProvider.notifier).state++;
    },
  );
});
