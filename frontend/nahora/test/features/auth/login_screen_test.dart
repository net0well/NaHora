import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:nahora/core/errors/app_exception.dart';
import 'package:nahora/core/storage/secure_token_storage.dart';
import 'package:nahora/core/utils/cpf_input_formatter.dart';
import 'package:nahora/features/auth/data/auth_repository.dart';
import 'package:nahora/features/auth/domain/authenticated_user.dart';
import 'package:nahora/features/auth/domain/session.dart';
import 'package:nahora/features/auth/presentation/auth_providers.dart';
import 'package:nahora/features/auth/presentation/login_screen.dart';

import '../../support/pump_design_system.dart';

void main() {
  group('CpfInputFormatter', () {
    test('Given digits, When formatted, Then the mask is applied as typed', () {
      // Arrange
      final formatter = CpfInputFormatter();

      TextEditingValue format(String text) => formatter.formatEditUpdate(
        TextEditingValue.empty,
        TextEditingValue(text: text),
      );

      // Act & Assert
      expect(format('111').text, '111');
      expect(format('111222').text, '111.222');
      expect(format('11122233344').text, '111.222.333-44');
    });

    test(
      'Given more than eleven digits, When typed, Then the extra is refused',
      () {
        // Arrange
        final formatter = CpfInputFormatter();
        const current = TextEditingValue(text: '111.222.333-44');

        // Act
        final result = formatter.formatEditUpdate(
          current,
          const TextEditingValue(text: '111.222.333-445'),
        );

        // Assert
        expect(result.text, current.text);
      },
    );

    test('Given a masked CPF, When stripped, Then only digits remain', () {
      // Arrange & Act & Assert — the API expects digits, and sending the mask
      // would be rejected as an invalid document
      expect(CpfInputFormatter.digitsOf('111.222.333-44'), '11122233344');
    });
  });

  group('LoginScreen', () {
    testWidgets(
      'Given the screen, When rendered, Then both fields and the action are present',
      (tester) async {
        // Arrange & Act
        await _pumpLogin(tester, _StubAuthRepository());

        // Assert
        expect(find.byKey(const Key('login_cpf')), findsOneWidget);
        expect(find.byKey(const Key('login_password')), findsOneWidget);
        expect(find.byKey(const Key('login_submit')), findsOneWidget);
      },
    );

    testWidgets(
      'Given credentials, When submitted, Then the CPF reaches the repository unmasked',
      (tester) async {
        // Arrange — the field shows 111.222.333-44 and the API must receive
        // 11122233344
        final repository = _StubAuthRepository();
        await _pumpLogin(tester, repository);

        // Act
        await tester.enterText(
          find.byKey(const Key('login_cpf')),
          '11122233344',
        );
        await tester.enterText(
          find.byKey(const Key('login_password')),
          'senha-secreta',
        );
        await tester.tap(find.byKey(const Key('login_submit')));
        await tester.pumpAndSettle();

        // Assert
        expect(repository.receivedCpf, '11122233344');
      },
    );

    testWidgets('Given wrong credentials, When submitted, '
        'Then the translated message is shown and the code is not', (
      tester,
    ) async {
      // Arrange
      final repository = _StubAuthRepository(
        failure: const BusinessException('INVALID_CREDENTIALS'),
      );
      await _pumpLogin(tester, repository);

      // Act
      await tester.enterText(find.byKey(const Key('login_cpf')), '11122233344');
      await tester.enterText(find.byKey(const Key('login_password')), 'errada');
      await tester.tap(find.byKey(const Key('login_submit')));
      await tester.pumpAndSettle();

      // Assert
      expect(find.text('CPF ou senha incorretos.'), findsOneWidget);
      expect(find.textContaining('INVALID_CREDENTIALS'), findsNothing);
    });

    testWidgets(
      'Given a failure, When it is shown, Then it is announced, not merely drawn',
      (tester) async {
        // Arrange — a screen reader user who taps Entrar and hears nothing has
        // no way to know the attempt failed
        await _pumpLogin(
          tester,
          _StubAuthRepository(
            failure: const BusinessException('INVALID_CREDENTIALS'),
          ),
        );

        // Act
        await tester.enterText(
          find.byKey(const Key('login_cpf')),
          '11122233344',
        );
        await tester.tap(find.byKey(const Key('login_submit')));
        await tester.pumpAndSettle();

        // Assert
        final semantics = tester.getSemantics(
          find.bySemanticsLabel('CPF ou senha incorretos.'),
        );
        expect(semantics.flagsCollection.isLiveRegion, isTrue);
      },
    );
  });
}

Future<void> _pumpLogin(WidgetTester tester, AuthRepository repository) async {
  await pumpScreen(
    tester,
    const LoginScreen(companyId: 'company-1'),
    size: const Size(390, 844),
    overrides: [
      authRepositoryProvider.overrideWithValue(repository),
      tokenStorageProvider.overrideWithValue(_NullTokenStorage()),
    ],
  );
}

/// A repository, not a mocked provider.
///
/// Overriding the provider's dependency rather than the provider itself keeps
/// the controller under test — a mocked controller would test nothing.
class _StubAuthRepository implements AuthRepository {
  _StubAuthRepository({this.failure});

  final AppException? failure;

  String? receivedCpf;

  @override
  Future<Session> signIn({
    required String cpf,
    required String password,
    required String companyId,
  }) async {
    receivedCpf = cpf;

    if (failure != null) {
      throw failure!;
    }

    return Session(
      user: const AuthenticatedUser(
        id: 'user-1',
        fullName: 'Maria Souza',
        role: 'Employee',
        permissions: [],
      ),
      pendingActions: const [],
      expiresAt: DateTime.now().add(const Duration(minutes: 15)),
    );
  }

  @override
  Future<Session?> restore() async => null;

  @override
  Future<void> signOut() async {}
}

class _NullTokenStorage implements TokenStorage {
  @override
  Future<void> clear() async {}

  @override
  Future<String?> readAccessToken() async => null;

  @override
  Future<String?> readRefreshToken() async => null;

  @override
  Future<void> saveTokens({
    required String accessToken,
    required String refreshToken,
  }) async {}
}
