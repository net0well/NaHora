import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:nahora/core/errors/app_exception.dart';
import 'package:nahora/features/attendance/presentation/attendance_providers.dart';
import 'package:nahora/features/attendance/presentation/punch_screen.dart';

import '../../support/fake_attendance_repository.dart';
import '../../support/fake_location_provider.dart';
import '../../support/pump_design_system.dart';

/// The hours shown above the punch button.
///
/// Every case here is about what the employee reads. None of them is about
/// whether they may punch — the schedule is information, and Art. 78 §1º
/// forbids turning it into a gate.
void main() {
  group('PunchScreen expected hours', () {
    late FakeAttendanceRepository attendance;

    setUp(() {
      attendance = FakeAttendanceRepository();
    });

    Future<void> pump(WidgetTester tester) => pumpScreen(
      tester,
      const PunchScreen(),
      overrides: [
        attendanceRepositoryProvider.overrideWithValue(attendance),
        locationProviderProvider.overrideWithValue(FakeLocationProvider()),
      ],
    );

    testWidgets('Given a working day, When shown, Then the hours are printed', (
      tester,
    ) async {
      // Arrange & Act
      await pump(tester);

      // Assert
      expect(find.byKey(const Key('punch_expected_hours')), findsOneWidget);
      expect(find.text('08:00 às 17:00, com 1h de intervalo'), findsOneWidget);
      expect(find.text('Jornada da empresa'), findsOneWidget);
    });

    testWidgets(
      'Given a day off, When shown, Then nothing is claimed about the hours',
      (tester) async {
        // Arrange — a Sunday. Printing "08:00 às 17:00" would assert a working
        // day nobody agreed to
        attendance.schedule = FakeAttendanceRepository.defaultSchedule(
          isWorkday: false,
        );

        // Act
        await pump(tester);

        // Assert
        expect(find.byKey(const Key('punch_expected_hours')), findsNothing);
      },
    );

    testWidgets('Given the schedule cannot be loaded, When shown, '
        'Then the punch button is still reachable', (tester) async {
      // Arrange — the schedule is context. An error above the punch button
      // would read as something standing between the employee and recording
      // their time.
      attendance.readFailure = const UnexpectedException();

      // Act
      await pump(tester);

      // Assert
      expect(find.byKey(const Key('punch_expected_hours')), findsNothing);
      expect(find.byKey(const Key('punch_button')), findsOneWidget);
    });
  });
}
