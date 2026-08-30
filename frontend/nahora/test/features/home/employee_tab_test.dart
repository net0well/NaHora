import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:nahora/core/errors/app_exception.dart';
import 'package:nahora/features/attendance/domain/punch_day.dart';
import 'package:nahora/features/attendance/domain/punch_direction.dart';
import 'package:nahora/features/attendance/domain/punch_trust.dart';
import 'package:nahora/features/attendance/presentation/attendance_providers.dart';
import 'package:nahora/features/home/presentation/employee_tab.dart';
import 'package:nahora/features/profile/presentation/profile_providers.dart';

import '../../support/fake_attendance_repository.dart';
import '../../support/fake_profile_repository.dart';
import '../../support/pump_design_system.dart';

void main() {
  group('EmployeeTab', () {
    late FakeAttendanceRepository attendance;
    late FakeProfileRepository profile;

    setUp(() {
      attendance = FakeAttendanceRepository();
      profile = FakeProfileRepository();
    });

    Future<void> pump(WidgetTester tester) => pumpScreen(
      tester,
      const EmployeeTab(),

      // Tall enough that every card is laid out. The tab scrolls on a real
      // handset; a viewport-sized test would only ever exercise the first card.
      size: const Size(390, 2400),
      overrides: [
        attendanceRepositoryProvider.overrideWithValue(attendance),
        profileRepositoryProvider.overrideWithValue(profile),
      ],
    );

    testWidgets('Given no punches today, When shown, '
        'Then it says so instead of inventing rows', (tester) async {
      // Arrange & Act — sample data on a compliance screen is worse than an
      // empty one: somebody eventually reads it as real
      await pump(tester);

      // Assert
      expect(find.byKey(const Key('employee_tab_empty')), findsOneWidget);
    });

    testWidgets(
      'Given punches today, When shown, Then each is listed in company time',
      (tester) async {
        // Arrange — the server sends UTC; São Paulo is three hours behind, so
        // 11:05Z has to render as 08:05
        attendance.day = PunchDay(
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
        expect(find.byKey(const Key('punch_entry_1')), findsOneWidget);
        expect(find.text('08:05'), findsOneWidget);
        expect(find.text('Próxima: Início do intervalo'), findsOneWidget);
      },
    );

    testWidgets('Given a degraded punch, When shown, '
        'Then the reason is written, not only coloured', (tester) async {
      // Arrange — roughly 8% of men cannot separate the tones, and the punch
      // still counts either way
      attendance.day = PunchDay(
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
        attendance.day = PunchDay(
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

    testWidgets(
      'Given the punch request fails, When shown, Then it offers a retry',
      (tester) async {
        // Arrange
        attendance.readFailure = const UnexpectedException();

        // Act
        await pump(tester);

        // Assert
        expect(find.text('Tentar novamente'), findsOneWidget);
      },
    );

    testWidgets(
      'Given the profile loads, When shown, Then the employee record is drawn',
      (tester) async {
        // Arrange & Act
        await pump(tester);

        // Assert
        expect(find.byKey(const Key('employee_tab_profile')), findsOneWidget);
        expect(find.text('529.982.247-25'), findsOneWidget);
        expect(find.text('Operadora'), findsOneWidget);
        expect(find.text('CLT'), findsOneWidget);
        expect(find.text('15/01/2026'), findsOneWidget);
      },
    );

    testWidgets(
      'Given the company loads, When shown, Then its registration data is drawn',
      (tester) async {
        // Arrange & Act
        await pump(tester);

        // Assert
        expect(find.byKey(const Key('employee_tab_company')), findsOneWidget);
        expect(find.text('11.222.333/0001-81'), findsOneWidget);
        expect(find.text('NaHora Desenvolvimento LTDA'), findsOneWidget);
        expect(find.text('ISENTO'), findsOneWidget);
        expect(find.text('CNPJ confirmado'), findsOneWidget);
      },
    );

    testWidgets(
      'Given a field the record does not have, When shown, Then no empty row',
      (tester) async {
        // Arrange & Act — a company legitimately without a municipal
        // registration must not read as data somebody should go and fix
        await pump(tester);

        // Assert
        expect(find.text('Inscrição municipal'), findsNothing);
      },
    );

    testWidgets(
      'Given a record missing its PIS, When shown, Then the gap is named',
      (tester) async {
        // Arrange — the employee should learn this here, not when an export
        // leaves them off it
        profile.employeeProfile = FakeProfileRepository.defaultEmployee(
          pis: null,
          isExportableToAfd: false,
        );

        // Act
        await pump(tester);

        // Assert
        expect(find.text('PIS pendente'), findsOneWidget);
      },
    );

    testWidgets(
      'Given the screen, When rendered, Then there is a way out of the app',
      (tester) async {
        // Arrange & Act — an app somebody cannot sign out of is an app that
        // hands the next person their colleague's record
        await pump(tester);

        // Assert
        expect(find.byKey(const Key('employee_tab_sign_out')), findsOneWidget);
      },
    );

    testWidgets(
      'Given the profile request fails, When shown, Then both cards offer a retry',
      (tester) async {
        // Arrange
        profile.failure = const UnexpectedException();

        // Act
        await pump(tester);

        // Assert
        expect(find.byKey(const Key('employee_tab_profile')), findsNothing);
        expect(find.text('Tentar novamente'), findsNWidgets(2));
      },
    );
  });
}
