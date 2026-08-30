import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:nahora/core/errors/app_exception.dart';
import 'package:nahora/core/network/api_client.dart';
import 'package:nahora/core/storage/secure_token_storage.dart';
import 'package:nahora/features/auth/data/auth_repository.dart';

import '../../support/fake_http_adapter.dart';

void main() {
  group('AuthRepository', () {
    late _InMemoryTokenStorage storage;

    setUp(() => storage = _InMemoryTokenStorage());

    AuthRepository build(FakeHttpAdapter adapter) => AuthRepository(
      client: ApiClient.create(
        storage: storage,
        baseUrl: 'https://api.test',
        onSessionLost: () async {},
        adapter: adapter,
      ),
      storage: storage,
    );

    Map<String, dynamic> loginBody() => {
      'accessToken': 'access-1',
      'refreshToken': 'refresh-1',
      'expiresIn': 900,
      'user': {
        'id': '0192f5c1-1111-7000-8000-000000000001',
        'fullName': 'Maria Souza',
        'role': 'Employee',
        'employeeId': '0192f5c1-2222-7000-8000-000000000002',
        'permissions': ['attendance:register'],
      },
      'pendingActions': ['GrantLocationPermission'],
    };

    test('Given valid credentials, When signing in, '
        'Then the tokens are stored and the session returned', () async {
      // Arrange
      final adapter = FakeHttpAdapter((_, _) => jsonResponse(200, loginBody()));

      // Act
      final session = await build(
        adapter,
      ).signIn(cpf: '11122233344', password: 'senha', companyId: 'company-1');

      // Assert
      expect(session.user.fullName, 'Maria Souza');
      expect(session.user.can('attendance:register'), isTrue);
      expect(session.mustGrantLocationPermission, isTrue);
      expect(await storage.readAccessToken(), 'access-1');
    });

    test('Given wrong credentials, When signing in, '
        'Then a business failure surfaces and nothing is stored', () async {
      // Arrange
      final adapter = FakeHttpAdapter(
        (_, _) => jsonResponse(401, {'code': 'INVALID_CREDENTIALS'}),
      );

      // Act & Assert
      await expectLater(
        build(adapter).signIn(cpf: '1', password: 'x', companyId: 'c'),
        throwsA(isA<AppException>()),
      );

      expect(await storage.readAccessToken(), isNull);
    });

    test(
      'Given no stored token, When restoring, Then there is simply no session',
      () async {
        // Arrange — opening the app for the first time is not an error, and it
        // must not reach the network to find that out
        final adapter = FakeHttpAdapter((_, _) => jsonResponse(200, {}));

        // Act
        final session = await build(adapter).restore();

        // Assert
        expect(session, isNull);
        expect(adapter.callCount, 0);
      },
    );

    test('Given a dead stored session, When restoring, Then the tokens are cleared', () async {
      // Arrange — leaving them would make every later call fail the same way
      await storage.saveTokens(accessToken: 'dead', refreshToken: 'dead');
      final adapter = FakeHttpAdapter(
        (_, _) => jsonResponse(401, {'code': 'UNAUTHORIZED'}),
      );

      // Act
      final session = await build(adapter).restore();

      // Assert
      expect(session, isNull);
      expect(await storage.readAccessToken(), isNull);
    });

    test('Given the server is unreachable, When signing out, '
        'Then the local session is cleared anyway', () async {
      // Arrange — a user who taps sign-out and stays signed in because the
      // network was down would be right to call that a bug, and on a shared
      // phone it is a security one
      await storage.saveTokens(
        accessToken: 'access-1',
        refreshToken: 'refresh-1',
      );

      final adapter = FakeHttpAdapter((_, _) {
        throw DioException(
          requestOptions: RequestOptions(path: '/api/v1/auth/logout'),
          type: DioExceptionType.connectionError,
        );
      });

      // Act
      await build(adapter).signOut();

      // Assert
      expect(await storage.readAccessToken(), isNull);
      expect(await storage.readRefreshToken(), isNull);
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
