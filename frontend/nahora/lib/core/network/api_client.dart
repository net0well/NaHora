import 'package:dio/dio.dart';

import '../config/app_config.dart';
import '../storage/secure_token_storage.dart';
import 'auth_interceptor.dart';

/// The application's HTTP client.
///
/// Only the data layer may reach this: a widget that calls the API directly
/// cannot be rendered in a test without a server, and the controller it skipped
/// is where the state belongs. A source-scanning test enforces that.
abstract final class ApiClient {
  /// Marks a request that must go out without a token and must never trigger a
  /// refresh — sign-in and refresh itself.
  static const String anonymousFlag = 'nahora.anonymous';

  /// Builds the client.
  ///
  /// [adapter] exists so a test can drive both this client and the refresh
  /// client it creates internally. Without it the refresh would be the one call
  /// in the exchange that escapes to the network, and the retry path — the part
  /// most worth testing — could not be covered at all.
  static Dio create({
    required TokenStorage storage,
    required Future<void> Function() onSessionLost,
    String? baseUrl,
    HttpClientAdapter? adapter,
  }) {
    final resolvedBaseUrl = baseUrl ?? AppConfig.apiBaseUrl;

    final client = Dio(_optionsFor(resolvedBaseUrl));
    final refreshClient = Dio(_optionsFor(resolvedBaseUrl));

    if (adapter != null) {
      client.httpClientAdapter = adapter;
      refreshClient.httpClientAdapter = adapter;
    }

    client.interceptors.add(
      AuthInterceptor(
        storage: storage,
        refreshClient: refreshClient,
        onSessionLost: onSessionLost,
      ),
    );

    return client;
  }

  static BaseOptions _optionsFor(String baseUrl) => BaseOptions(
    baseUrl: baseUrl,
    connectTimeout: AppConfig.requestTimeout,
    receiveTimeout: AppConfig.requestTimeout,
    sendTimeout: AppConfig.requestTimeout,
    contentType: 'application/json',

    // Error statuses must raise, because that is the only path the auth
    // interceptor can act on: a 401 that returns normally never reaches
    // onError, and the session would never renew. The response body survives
    // on the raised DioException, so the 422 field errors a form needs are
    // still there for the mapper to read.
    validateStatus: (status) => status != null && status < 400,
  );
}
