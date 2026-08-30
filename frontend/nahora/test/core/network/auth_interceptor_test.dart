import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:nahora/core/network/api_client.dart';
import 'package:nahora/core/storage/secure_token_storage.dart';

import '../../support/fake_http_adapter.dart';

/// The interceptor that renews an expired session.
///
/// This is where a mistake is expensive: retry without a limit and a dead
/// session becomes a loop hammering the server, refresh concurrently and the
/// rotated tokens race each other into what the server reads as token theft.
void main() {
  group('AuthInterceptor', () {
    late _InMemoryTokenStorage storage;
    late int sessionLostCount;

    setUp(() {
      storage = _InMemoryTokenStorage();
      sessionLostCount = 0;
    });

    // One adapter drives both this client and the refresh client built inside
    // it, so a single script covers the whole exchange.
    Dio buildClient(FakeHttpAdapter adapter) => ApiClient.create(
      storage: storage,
      baseUrl: 'https://api.test',
      onSessionLost: () async => sessionLostCount++,
      adapter: adapter,
    );

    test('Given a request with a stored token, When it is sent, '
        'Then the token travels in the Authorization header', () async {
      // Arrange
      await storage.saveTokens(
        accessToken: 'access-1',
        refreshToken: 'refresh-1',
      );
      final adapter = FakeHttpAdapter(
        (options, _) => jsonResponse(200, {'ok': true}),
      );

      // Act
      await buildClient(adapter).get<dynamic>('/api/v1/punches');

      // Assert
      expect(
        adapter.requests.single.headers['Authorization'],
        'Bearer access-1',
      );
    });

    test('Given a sign-in request, When it is sent, '
        'Then it carries no token and cannot trigger a refresh', () async {
      // Arrange — a 401 on sign-in means wrong credentials, not an expired
      // session. Refreshing here would be nonsense.
      await storage.saveTokens(
        accessToken: 'access-1',
        refreshToken: 'refresh-1',
      );
      final adapter = FakeHttpAdapter(
        (options, _) => jsonResponse(401, {'code': 'INVALID_CREDENTIALS'}),
      );

      final client = buildClient(adapter);

      // Act
      await expectLater(
        client.post<dynamic>(
          '/api/v1/auth/login',
          options: Options(extra: {ApiClient.anonymousFlag: true}),
        ),
        throwsA(isA<DioException>()),
      );

      // Assert
      expect(adapter.callCount, 1, reason: 'no refresh, no retry');
      expect(
        adapter.requests.single.headers.containsKey('Authorization'),
        isFalse,
      );
      expect(sessionLostCount, 0);
    });

    test('Given an expired token, When a request returns 401, '
        'Then the session is refreshed and the request retried once', () async {
      // Arrange
      await storage.saveTokens(accessToken: 'stale', refreshToken: 'refresh-1');

      final adapter = FakeHttpAdapter((options, call) {
        if (options.path == '/api/v1/auth/refresh') {
          return jsonResponse(200, {
            'accessToken': 'fresh',
            'refreshToken': 'refresh-2',
          });
        }

        // First attempt is rejected; the retry, carrying the new token, is
        // accepted.
        return options.headers['Authorization'] == 'Bearer fresh'
            ? jsonResponse(200, {'ok': true})
            : jsonResponse(401, {'code': 'UNAUTHORIZED'});
      });

      // Act
      final response = await buildClient(adapter)
          .get<dynamic>('/api/v1/punches');

      // Assert
      expect(response.statusCode, 200);
      expect(adapter.callsTo('/api/v1/auth/refresh'), 1);
      expect(
        adapter.callsTo('/api/v1/punches'),
        2,
        reason: 'original plus one retry',
      );

      // The rotated token replaced the old one, or the next refresh would
      // replay a token the server has already revoked.
      expect(await storage.readAccessToken(), 'fresh');
      expect(await storage.readRefreshToken(), 'refresh-2');
      expect(sessionLostCount, 0);
    });

    test('Given a retry that also returns 401, When it happens, '
        'Then it stops rather than looping', () async {
      // Arrange — the failure mode that matters. Without the retry flag this
      // spins until something gives up.
      await storage.saveTokens(accessToken: 'stale', refreshToken: 'refresh-1');

      final adapter = FakeHttpAdapter((options, _) {
        if (options.path == '/api/v1/auth/refresh') {
          return jsonResponse(200, {
            'accessToken': 'fresh',
            'refreshToken': 'refresh-2',
          });
        }

        return jsonResponse(401, {'code': 'UNAUTHORIZED'});
      });

      // Act
      await expectLater(
        buildClient(adapter).get<dynamic>('/api/v1/punches'),
        throwsA(isA<DioException>()),
      );

      // Assert
      expect(
        adapter.callsTo('/api/v1/punches'),
        2,
        reason: 'exactly one retry',
      );
      expect(adapter.callsTo('/api/v1/auth/refresh'), 1);
    });

    test('Given a refresh that fails, When it happens, '
        'Then the session is dropped and the caller told', () async {
      // Arrange
      await storage.saveTokens(accessToken: 'stale', refreshToken: 'expired');

      final adapter = FakeHttpAdapter(
        (options, _) => jsonResponse(401, {'code': 'UNAUTHORIZED'}),
      );

      // Act
      await expectLater(
        buildClient(adapter).get<dynamic>('/api/v1/punches'),
        throwsA(isA<DioException>()),
      );

      // Assert
      expect(sessionLostCount, 1, reason: 'every screen has to react at once');
      expect(
        adapter.callsTo('/api/v1/punches'),
        1,
        reason: 'no retry without a token',
      );
    });

    test('Given three requests failing at once, When they refresh, '
        'Then the refresh happens once', () async {
      // Arrange — concurrent refreshes rotate the token out from under each
      // other, and the server reads a replayed refresh token as theft and
      // revokes the whole family.
      await storage.saveTokens(accessToken: 'stale', refreshToken: 'refresh-1');

      final adapter = FakeHttpAdapter((options, _) {
        if (options.path == '/api/v1/auth/refresh') {
          return jsonResponse(200, {
            'accessToken': 'fresh',
            'refreshToken': 'refresh-2',
          });
        }

        return options.headers['Authorization'] == 'Bearer fresh'
            ? jsonResponse(200, {'ok': true})
            : jsonResponse(401, {'code': 'UNAUTHORIZED'});
      });

      final client = buildClient(adapter);

      // Act
      await Future.wait([
        client.get<dynamic>('/api/v1/a'),
        client.get<dynamic>('/api/v1/b'),
        client.get<dynamic>('/api/v1/c'),
      ]);

      // Assert
      expect(adapter.callsTo('/api/v1/auth/refresh'), 1);
    });
  });
}

class _InMemoryTokenStorage implements TokenStorage {
  String? _access;
  String? _refresh;

  @override
  Future<void> clear() async {
    _access = null;
    _refresh = null;
  }

  @override
  Future<String?> readAccessToken() async => _access;

  @override
  Future<String?> readRefreshToken() async => _refresh;

  @override
  Future<void> saveTokens({
    required String accessToken,
    required String refreshToken,
  }) async {
    _access = accessToken;
    _refresh = refreshToken;
  }
}
