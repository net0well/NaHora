import 'package:dio/dio.dart';

import '../../../core/errors/app_exception.dart';
import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception_mapper.dart';
import '../../../core/storage/secure_token_storage.dart';
import '../domain/authenticated_user.dart';
import '../domain/session.dart';

/// Sign-in, sign-out, and the session that survives a restart.
///
/// The only place that knows both the API and the token store. A controller
/// asks for a session; it never learns that one lives behind HTTP and the other
/// behind the platform vault.
// Named parameters keep the call site readable while the fields stay private.
// An initializing formal would expose `_client` as the parameter name.
// ignore_for_file: prefer_initializing_formals
class AuthRepository {
  AuthRepository({required Dio client, required TokenStorage storage})
    : _client = client,
      _storage = storage;

  final Dio _client;
  final TokenStorage _storage;

  Future<Session> signIn({
    required String cpf,
    required String password,
    required String companyId,
  }) async {
    try {
      final response = await _client.post<Map<String, dynamic>>(
        '/api/v1/auth/login',
        data: {'cpf': cpf, 'password': password, 'companyId': companyId},

        // Sign-in carries no token and must not trigger a refresh: a 401 here
        // means wrong credentials, not an expired session.
        options: Options(extra: {ApiClient.anonymousFlag: true}),
      );

      final body = response.data;
      if (body == null) {
        throw const UnexpectedException();
      }

      await _storage.saveTokens(
        accessToken: body['accessToken'] as String,
        refreshToken: body['refreshToken'] as String,
      );

      return _readSession(body);
    } on DioException catch (exception) {
      throw ApiExceptionMapper.map(exception);
    }
  }

  /// Restores the session on start-up, if the stored token is still good.
  ///
  /// Returns null rather than throwing when there is no session: opening the
  /// app for the first time is not an error.
  Future<Session?> restore() async {
    final token = await _storage.readAccessToken();
    if (token == null) {
      return null;
    }

    try {
      final response = await _client.get<Map<String, dynamic>>(
        '/api/v1/auth/me',
      );
      final body = response.data;

      return body == null ? null : _readSession(body);
    } on DioException catch (exception) {
      final mapped = ApiExceptionMapper.map(exception);

      // A dead session is cleared rather than carried around: leaving the
      // tokens would make every later call fail the same way.
      if (mapped is UnauthorizedException) {
        await _storage.clear();

        return null;
      }

      throw mapped;
    }
  }

  /// Ends the session on the server, then locally.
  ///
  /// Local credentials are cleared even when the server call fails. A user who
  /// taps sign-out and stays signed in because the network was down would be
  /// right to call that a bug — and on a shared phone it is a security one.
  Future<void> signOut() async {
    try {
      final refreshToken = await _storage.readRefreshToken();
      if (refreshToken != null) {
        await _client.post<void>(
          '/api/v1/auth/logout',
          data: {'refreshToken': refreshToken},
        );
      }
    } on DioException {
      // Deliberately swallowed. See above.
    } finally {
      await _storage.clear();
    }
  }

  /// Reads a session from either shape the API returns.
  ///
  /// Sign-in nests the person under `user` and adds the tokens; `auth/me`
  /// returns the person at the top level. One reader rather than two, because
  /// the differences are two keys and duplicating the mapping is how the two
  /// drift apart.
  static Session _readSession(Map<String, dynamic> body) {
    final user = (body['user'] ?? body) as Map<String, dynamic>;

    return Session(
      user: AuthenticatedUser(
        id: user['id'] as String,
        fullName: user['fullName'] as String,
        role: user['role'] as String,
        employeeId: user['employeeId'] as String?,
        permissions: (user['permissions'] as List<dynamic>? ?? const [])
            .map((permission) => permission.toString())
            .toList(),
      ),
      pendingActions: (body['pendingActions'] as List<dynamic>? ?? const [])
          .map((action) => action.toString())
          .toList(),
      expiresAt: DateTime.now().add(
        Duration(seconds: (body['expiresIn'] as int?) ?? 0),
      ),
    );
  }
}
