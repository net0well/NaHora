import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:nahora/core/errors/app_exception.dart';
import 'package:nahora/features/attendance/domain/punch_direction.dart';
import 'package:nahora/features/attendance/domain/punch_history_page.dart';
import 'package:nahora/features/attendance/presentation/attendance_providers.dart';
import 'package:nahora/features/home/presentation/history_tab.dart';

import '../../support/fake_attendance_repository.dart';
import '../../support/pump_design_system.dart';

void main() {
  group('HistoryTab', () {
    late FakeAttendanceRepository repository;

    setUp(() {
      repository = FakeAttendanceRepository();
    });

    Future<void> pump(WidgetTester tester) => pumpScreen(
      tester,
      const HistoryTab(),
      overrides: [attendanceRepositoryProvider.overrideWithValue(repository)],
    );

    testWidgets('Given nothing recorded, When shown, Then it says so', (
      tester,
    ) async {
      // Arrange & Act
      await pump(tester);

      // Assert
      expect(find.byKey(const Key('history_tab_empty')), findsOneWidget);
    });

    testWidgets('Given punches across two days, When shown, '
        'Then each day gets its own heading', (tester) async {
      // Arrange — grouped by the day the server decided, not by one
      // recomputed from the timestamp on a device in another timezone
      repository.historyPage = PunchHistoryPage(
        page: 1,
        pageSize: 30,
        totalCount: 3,
        hasMore: false,
        entries: [
          FakeAttendanceRepository.entry(
            nsr: 3,
            direction: PunchDirection.exit,
            occurredAt: DateTime.utc(2026, 8, 30, 21, 2),
          ),
          FakeAttendanceRepository.entry(
            nsr: 2,
            occurredAt: DateTime.utc(2026, 8, 30, 11, 5),
          ),
          FakeAttendanceRepository.entry(
            nsr: 1,
            occurredAt: DateTime.utc(2026, 8, 29, 11, 30),
            localDate: '2026-08-29',
          ),
        ],
      );

      // Act
      await pump(tester);

      // Assert
      expect(find.text('30/08/2026'), findsOneWidget);
      expect(find.text('29/08/2026'), findsOneWidget);
      expect(find.byKey(const Key('history_tab_list')), findsOneWidget);
    });

    testWidgets(
      'Given a punch, When shown, Then its time is in company hours',
      (tester) async {
        // Arrange — 11:05Z is 08:05 in São Paulo, and the employee's record
        // must not disagree with what they were shown
        repository.historyPage = PunchHistoryPage(
          page: 1,
          pageSize: 30,
          totalCount: 1,
          hasMore: false,
          entries: [
            FakeAttendanceRepository.entry(
              occurredAt: DateTime.utc(2026, 8, 30, 11, 5),
            ),
          ],
        );

        // Act
        await pump(tester);

        // Assert
        expect(find.text('08:05'), findsOneWidget);
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
      expect(find.byKey(const Key('history_tab_list')), findsNothing);
    });
  });
}
