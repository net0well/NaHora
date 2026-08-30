import 'package:flutter_secure_storage/flutter_secure_storage.dart';

/// Session token store.
///
/// Backed by the platform credential vault — DPAPI on Windows, Keychain on
/// iOS/macOS, EncryptedSharedPreferences on Android. Tokens must never touch
/// SharedPreferences: it is a plaintext XML file that any process running as the
/// user can read (OWASP Mobile M9).
abstract interface class TokenStorage {
  Future<void> saveTokens({
    required String accessToken,
    required String refreshToken,
  });

  Future<String?> readAccessToken();

  Future<String?> readRefreshToken();

  Future<void> clear();
}

class SecureTokenStorage implements TokenStorage {
  SecureTokenStorage({FlutterSecureStorage? storage})
    : _storage =
          storage ??
          const FlutterSecureStorage(
            // Android encrypts by default from v10 onwards. iOS is pinned to
            // this-device-only so a restored backup cannot carry a live
            // session onto another handset.
            iOptions: IOSOptions(
              accessibility: KeychainAccessibility.first_unlock_this_device,
            ),
          );

  static const String _accessTokenKey = 'nahora.access_token';
  static const String _refreshTokenKey = 'nahora.refresh_token';

  final FlutterSecureStorage _storage;

  @override
  Future<void> saveTokens({
    required String accessToken,
    required String refreshToken,
  }) async {
    await _storage.write(key: _accessTokenKey, value: accessToken);
    await _storage.write(key: _refreshTokenKey, value: refreshToken);
  }

  @override
  Future<String?> readAccessToken() => _storage.read(key: _accessTokenKey);

  @override
  Future<String?> readRefreshToken() => _storage.read(key: _refreshTokenKey);

  @override
  Future<void> clear() async {
    await _storage.delete(key: _accessTokenKey);
    await _storage.delete(key: _refreshTokenKey);
  }
}
