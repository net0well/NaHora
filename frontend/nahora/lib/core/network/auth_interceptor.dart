import 'dart:async';

import 'package:dio/dio.dart';

import '../storage/secure_token_storage.dart';

/// Attaches the session token, and renews it once when the server says it has
/// expired.
///
/// The retry is deliberately limited to **one** attempt per request. A refresh
/// that itself returns 401 means the session is gone, and retrying would turn a
/// dead session into a loop that hammers the server while the user watches a
/// spinner.
// Named parameters keep the call site readable while the fields stay private.
// An initializing formal would expose `_storage` as the parameter name.
// ignore_for_file: prefer_initializing_formals
class AuthInterceptor extends Interceptor {
  AuthInterceptor({
    required TokenStorage storage,
    required Dio refreshClient,
    required Future<void> Function() onSessionLost,
  }) : _storage = storage,
       _refreshClient = refreshClient,
       _onSessionLost = onSessionLost;

  static const String _retriedFlag = 'nahora.retried';

  final TokenStorage _storage;

  /// A separate Dio without this interceptor.
  ///
  /// Refreshing through the intercepted client would re-enter this class when
  /// the refresh itself answers 401, and recursion inside an interceptor is the
  /// classic way to produce an infinite retry.
  final Dio _refreshClient;

  final Future<void> Function() _onSessionLost;

  /// Serialises concurrent refreshes.
  ///
  /// Three requests failing at once must produce one refresh, not three. The
  /// second and third await the first instead of racing it, which also stops
  /// them from rotating the refresh token out from under each other — the
  /// server treats a replayed refresh token as theft and revokes the family.
  Future<bool>? _inFlightRefresh;

  @override
  Future<void> onRequest(
    RequestOptions options,
    RequestInterceptorHandler handler,
  ) async {
    if (options.extra['nahora.anonymous'] != true) {
      final token = await _storage.readAccessToken();
      if (token != null) {
        options.headers['Authorization'] = 'Bearer $token';
      }
    }

    handler.next(options);
  }

  @override
  Future<void> onError(
    DioException err,
    ErrorInterceptorHandler handler,
  ) async {
    final request = err.requestOptions;

    final shouldRetry =
        err.response?.statusCode == 401 &&
        request.extra[_retriedFlag] != true &&
        request.extra['nahora.anonymous'] != true;

    if (!shouldRetry) {
      handler.next(err);
      return;
    }

    final renewed = await _refreshOnce();
    if (!renewed) {
      await _onSessionLost();
      handler.next(err);
      return;
    }

    try {
      request.extra[_retriedFlag] = true;
      final token = await _storage.readAccessToken();
      request.headers['Authorization'] = 'Bearer $token';

      handler.resolve(await _refreshClient.fetch<dynamic>(request));
    } on DioException catch (retryError) {
      handler.next(retryError);
    }
  }

  Future<bool> _refreshOnce() {
    // Whoever gets here first starts the refresh; everyone else waits on the
    // same future and reads the same answer.
    return _inFlightRefresh ??= _performRefresh().whenComplete(() {
      _inFlightRefresh = null;
    });
  }

  Future<bool> _performRefresh() async {
    final refreshToken = await _storage.readRefreshToken();
    if (refreshToken == null) {
      return false;
    }

    try {
      final response = await _refreshClient.post<Map<String, dynamic>>(
        '/api/v1/auth/refresh',
        data: {'refreshToken': refreshToken},
      );

      final body = response.data;
      final accessToken = body?['accessToken'];
      final rotated = body?['refreshToken'];

      if (accessToken is! String || rotated is! String) {
        return false;
      }

      await _storage.saveTokens(
        accessToken: accessToken,
        refreshToken: rotated,
      );

      return true;
    } on DioException {
      return false;
    }
  }
}
