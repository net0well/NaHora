import 'package:faceid/core/errors/app_exception.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('ErrorMessages', () {
    test('Given a known code, When resolve is called, Then returns the mapped message', () {
      // Arrange
      const code = 'INVALID_CREDENTIALS';

      // Act
      final message = ErrorMessages.resolve(code);

      // Assert
      expect(message, 'CPF ou senha incorretos.');
    });

    test('Given an unknown code, When resolve is called, Then returns the generic message', () {
      // Arrange
      const code = 'SOME_CODE_THE_APP_DOES_NOT_KNOW';

      // Act
      final message = ErrorMessages.resolve(code);

      // Assert
      expect(
        message,
        'Não foi possível concluir a operação. Tente novamente em instantes.',
      );
    });

    test('Given an unknown code, When resolve is called, Then the raw code does not leak', () {
      // Arrange
      const code = 'DB_CONSTRAINT_VIOLATION_employee_cpf_unique';

      // Act
      final message = ErrorMessages.resolve(code);

      // Assert — technical detail belongs in the log, never on screen
      expect(message.contains(code), isFalse);
      expect(message.contains('_'), isFalse);
    });

    test('Given any mapped code, When resolved, Then no message exposes a biometric score', () {
      // Arrange
      const forbidden = ['score', 'similarity', 'liveness', 'threshold', 'embedding'];

      // Act & Assert — a message that reveals the score teaches an attacker
      // what to tune. The user is told what to do, never what was measured.
      for (final code in _allMappedCodes) {
        final message = ErrorMessages.resolve(code).toLowerCase();
        for (final term in forbidden) {
          expect(
            message.contains(term),
            isFalse,
            reason: 'message for "$code" must not mention "$term"',
          );
        }
      }
    });
  });
}

const _allMappedCodes = [
  'NETWORK_ERROR',
  'UNAUTHORIZED',
  'INVALID_CREDENTIALS',
  'ACCOUNT_LOCKED',
  'FACE_NOT_DETECTED',
  'MULTIPLE_FACES_DETECTED',
  'FACE_QUALITY_TOO_LOW',
  'LIVENESS_FAILED',
  'LIVENESS_CHALLENGE_TIMEOUT',
  'FACE_ENROLLMENT_INCONSISTENT',
  'BIOMETRIC_CONSENT_REQUIRED',
  'EMPLOYEE_PROFILE_REQUIRED',
];
