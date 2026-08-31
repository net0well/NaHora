import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:nahora/features/attendance/domain/punch_location.dart';
import 'package:nahora/features/attendance/presentation/attendance_providers.dart';
import 'package:nahora/features/attendance/presentation/day_locations_sheet.dart';

import '../../support/fake_attendance_repository.dart';
import '../../support/pump_design_system.dart';

/// Looking at where a day's marks were made.
///
/// The sheet is the only thing in the app that pulls a coordinate down, so the
/// cases worth pinning are the ones where there is nothing to pull.
void main() {
  group('DayLocationsSheet', () {
    late FakeAttendanceRepository repository;

    setUp(() {
      repository = FakeAttendanceRepository();
    });

    Future<void> pump(WidgetTester tester, {required List<dynamic> entries}) =>
        pumpScreen(
          tester,
          Scaffold(
            body: DayLocationsSheet(
              dayLabel: '30/08/2026',
              entries: [...entries.cast()],
            ),
          ),
          size: const Size(390, 900),
          overrides: [
            attendanceRepositoryProvider.overrideWithValue(repository),
          ],
        );

    testWidgets('Given a day with marks, When opened, '
        'Then the first one is mapped and the others are offered', (
      tester,
    ) async {
      // Arrange
      repository.locations['punch-1'] = PunchLocation(
        punchId: 'punch-1',
        latitude: -23.5505,
        longitude: -46.6333,
        accuracyMeters: 12,
        capturedAt: _capturedAt,
      );

      // Act
      await pump(
        tester,
        entries: [
          FakeAttendanceRepository.entry(nsr: 1),
          FakeAttendanceRepository.entry(
            nsr: 2,
            occurredAt: DateTime.utc(2026, 8, 30, 15, 5),
          ),
        ],
      );

      // Assert
      expect(find.byKey(const Key('day_location_map')), findsOneWidget);
      expect(
        find.byKey(const Key('day_location_hour_punch-1')),
        findsOneWidget,
      );
      expect(
        find.byKey(const Key('day_location_hour_punch-2')),
        findsOneWidget,
      );
    });

    testWidgets('Given a day of inclusions only, When opened, '
        'Then it says there is nothing to map', (tester) async {
      // Arrange & Act — an inclusion was never captured, so there is no
      // position to ask the server for
      await pump(tester, entries: [FakeAttendanceRepository.included()]);

      // Assert
      expect(find.byKey(const Key('day_locations_empty')), findsOneWidget);
      expect(find.byKey(const Key('day_location_map')), findsNothing);
    });

    testWidgets('Given the position cannot be fetched, When opened, '
        'Then it states so instead of failing', (tester) async {
      // Arrange & Act — the mark itself is unaffected, so this is not an error
      // worth a red banner
      await pump(tester, entries: [FakeAttendanceRepository.entry(nsr: 1)]);

      // Assert
      expect(find.text('Sem sinal de GPS'), findsOneWidget);
    });

    testWidgets('Given another hour, When picked, Then that mark is fetched', (
      tester,
    ) async {
      // Arrange
      repository.locations['punch-1'] = PunchLocation(
        punchId: 'punch-1',
        latitude: -23.5505,
        longitude: -46.6333,
        accuracyMeters: 12,
        capturedAt: _capturedAt,
      );
      repository.locations['punch-2'] = PunchLocation(
        punchId: 'punch-2',
        latitude: -23.6,
        longitude: -46.7,
        accuracyMeters: 40,
        capturedAt: _capturedAt,
      );

      await pump(
        tester,
        entries: [
          FakeAttendanceRepository.entry(nsr: 1),
          FakeAttendanceRepository.entry(
            nsr: 2,
            occurredAt: DateTime.utc(2026, 8, 30, 15, 5),
          ),
        ],
      );

      // Act
      await tester.tap(find.byKey(const Key('day_location_hour_punch-2')));
      await tester.pumpAndSettle();

      // Assert
      expect(repository.requestedLocations, ['punch-1', 'punch-2']);
    });
  });
}

/// When the device read the position, which the sheet does not render but
/// the model requires.
final _capturedAt = DateTime.utc(2026, 8, 30, 11, 5);
