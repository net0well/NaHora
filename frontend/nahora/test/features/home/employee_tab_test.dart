import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:nahora/core/errors/app_exception.dart';
import 'package:nahora/features/attendance/domain/punch_day.dart';
import 'package:nahora/features/attendance/domain/punch_direction.dart';
import 'package:nahora/features/attendance/domain/punch_trust.dart';
import 'package:nahora/features/attendance/presentation/attendance_providers.dart';
import 'package:nahora/features/home/presentation/employee_tab.dart';

import '../../support/fake_attendance_repository.dart';
import '../../support/pump_design_system.dart';

void main() {
  group('EmployeeTab', () {
    late FakeAttendanceRepository repository;

    setUp(() {
      repository = FakeAttendanceRepository();
    });

    Future<void> pump(WidgetTester tester) => pumpScreen(
      tester,
      const EmployeeTab(),
      overrides: [attendanceRepositoryProvider.overrideWithValue(repository)],
    );

    testWidgets('Given no punches today, When shown, '
        'Then it says so instead of inventing rows', (tester) async {
      // Arrange & Act — sample data on a compliance screen is worse than an
      // empty one: somebody eventually reads it as real
      await pump(tester);

      // Assert
      expect(find.byKey(const Key('employee_tab_empty')), findsOneWidget);
      expect(find.byKey(const Key('employee_tab_list')), findsNothing);
    });

    testWidgets(
      'Given punches today, When shown, Then each one is listed in company time',
      (tester) async {
        // Arrange — the server sends UTC; São Paulo is three hours behind, so
        // 11:05Z has to render as 08:05
        repository.day = PunchDay(
          localDate: '2026-08-30',
          timeZoneId: 'America/Sao_Paulo',
          nextDirection: PunchDirection.breakStart,
          entries: [
            FakeAttendanceRepository.entry(
              occurredAt: DateTime.utc(2026, 8, 30, 11, 5),
            ),
          ],
        );

        // Act
        await pump(tester);

        // Assert
        expect(find.byKey(const Key('employee_tab_list')), findsOneWidget);
        expect(find.text('08:05'), findsOneWidget);
        expect(find.text('Entrada'), findsOneWidget);
      },
    );

    testWidgets(
      'Given punches today, When shown, Then the next direction is named',
      (tester) async {
        // Arrange — the employee should know what the button will record
        // before pressing it, not after
        repository.day = PunchDay(
          localDate: '2026-08-30',
          timeZoneId: 'America/Sao_Paulo',
          nextDirection: PunchDirection.breakStart,
          entries: [FakeAttendanceRepository.entry()],
        );

        // Act
        await pump(tester);

        // Assert
        expect(find.text('Próxima: Início do intervalo'), findsOneWidget);
      },
    );

    testWidgets('Given a degraded punch, When shown, '
        'Then the reason is written, not only coloured', (tester) async {
      // Arrange — roughly 8% of men cannot separate the tones, and the punch
      // still counts either way
      repository.day = PunchDay(
        localDate: '2026-08-30',
        timeZoneId: 'America/Sao_Paulo',
        nextDirection: PunchDirection.breakStart,
        entries: [FakeAttendanceRepository.entry(trust: PunchTrust.degraded)],
      );

      // Act
      await pump(tester);

      // Assert
      expect(find.textContaining('Precisão baixa'), findsOneWidget);
    });

    testWidgets(
      'Given an offline punch, When shown, Then it reads as recorded, not failed',
      (tester) async {
        // Arrange — offline is ordinary, not suspicious: it is what the app
        // does in a basement
        repository.day = PunchDay(
          localDate: '2026-08-30',
          timeZoneId: 'America/Sao_Paulo',
          nextDirection: PunchDirection.breakStart,
          entries: [FakeAttendanceRepository.entry(isOffline: true)],
        );

        // Act
        await pump(tester);

        // Assert
        expect(find.textContaining('Registrada offline'), findsOneWidget);
      },
    );

    testWidgets('Given the request fails, When shown, Then it offers a retry', (
      tester,
    ) async {
      // Arrange
      repository.readFailure = const UnexpectedException();

      // Act
      await pump(tester);

      // Assert
      expect(find.text('Tentar novamente'), findsOneWidget);
      expect(find.byKey(const Key('employee_tab_list')), findsNothing);
    });
  });
}
