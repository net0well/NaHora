import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:nahora/core/errors/app_exception.dart';
import 'package:nahora/features/adjustments/presentation/adjustments_providers.dart';
import 'package:nahora/features/adjustments/presentation/request_adjustment_sheet.dart';
import 'package:nahora/features/attendance/domain/punch_entry.dart';

import '../../support/fake_adjustments_repository.dart';
import '../../support/fake_attachment_picker.dart';
import '../../support/fake_attendance_repository.dart';
import '../../support/pump_design_system.dart';

/// Asking for a correction.
///
/// Nothing here changes a mark: it sends a request a manager decides. The cases
/// worth pinning are the ones where the form must refuse, and the one where the
/// request lands but its photo does not.
void main() {
  group('RequestAdjustmentSheet', () {
    late FakeAdjustmentsRepository repository;
    late FakeAttachmentPicker picker;

    setUp(() {
      repository = FakeAdjustmentsRepository();
      picker = FakeAttachmentPicker();
    });

    Future<void> pump(WidgetTester tester, {List<PunchEntry>? entries}) =>
        pumpScreen(
          tester,
          Scaffold(
            body: RequestAdjustmentSheet(
              dayLabel: '30/08/2026',
              localDate: '2026-08-30',
              entries: entries ?? [FakeAttendanceRepository.entry(nsr: 1)],
            ),
          ),
          size: const Size(390, 1600),
          overrides: [
            adjustmentsRepositoryProvider.overrideWithValue(repository),
            attachmentPickerProvider.overrideWithValue(picker),
          ],
        );

    testWidgets('Given the form, When opened on a mark, '
        'Then its time is already filled in', (tester) async {
      // Arrange & Act — asking for 08:05 instead of 08:32 should be typing one
      // change, not both
      await pump(tester);

      // Assert
      expect(find.text('08:05'), findsWidgets);
      expect(find.byKey(const Key('request_target_punch-1')), findsOneWidget);
      expect(find.byKey(const Key('request_target_inclusion')), findsOneWidget);
    });

    testWidgets('Given no reason chosen, When shown, '
        'Then the request cannot be sent', (tester) async {
      // Arrange & Act — a correction with no reason is a correction an audit
      // cannot read
      await pump(tester);

      // Assert
      final button = tester.widget<Widget>(
        find.byKey(const Key('request_submit')),
      );
      expect(button, isNotNull);
      expect(repository.asked, isEmpty);

      await tester.tap(find.byKey(const Key('request_submit')));
      await tester.pumpAndSettle();
      expect(repository.asked, isEmpty);
    });

    testWidgets('Given a reason and a time, When sent, '
        'Then the correction points at the mark it replaces', (tester) async {
      // Arrange
      await pump(tester);

      // Act
      await tester.tap(
        find.byKey(const Key('request_justification_FORGOT_TO_PUNCH')),
      );
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const Key('request_submit')));
      await tester.pumpAndSettle();

      // Assert
      final asked = repository.asked.single;
      expect(asked.punchId, 'punch-1');
      expect(asked.justificationId, FakeAdjustmentsRepository.forgot.id);

      // 08:05 in São Paulo is 11:05 UTC. The offset comes from the mark, not
      // from the device, so a phone in another timezone asks for the same hour.
      expect(asked.occurredAt.toUtc(), DateTime.utc(2026, 8, 30, 11, 5));
      expect(find.byKey(const Key('request_sent')), findsOneWidget);
    });

    testWidgets('Given an inclusion, When sent, '
        'Then it names the boundary instead of a mark', (tester) async {
      // Arrange
      await pump(tester);

      // Act
      await tester.tap(find.byKey(const Key('request_target_inclusion')));
      await tester.pumpAndSettle();
      await tester.tap(
        find.byKey(const Key('request_justification_FORGOT_TO_PUNCH')),
      );
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const Key('request_submit')));
      await tester.pumpAndSettle();

      // Assert
      final asked = repository.asked.single;
      expect(asked.punchId, isNull);
      expect(asked.direction, 'Entry');
    });

    testWidgets('Given the catch-all reason, When nothing is written, '
        'Then it cannot be sent', (tester) async {
      // Arrange — without text "Outro" becomes the way to avoid choosing
      await pump(tester);

      // Act
      await tester.tap(find.byKey(const Key('request_justification_OTHER')));
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const Key('request_submit')));
      await tester.pumpAndSettle();

      // Assert
      expect(repository.asked, isEmpty);
      expect(find.text('Observação (obrigatória)'), findsOneWidget);
    });

    testWidgets('Given a photo is chosen, When sent, '
        'Then it goes up after the request', (tester) async {
      // Arrange
      await pump(tester);

      // Act
      await tester.tap(find.byKey(const Key('request_attachment_camera')));
      await tester.pumpAndSettle();
      await tester.tap(
        find.byKey(const Key('request_justification_FORGOT_TO_PUNCH')),
      );
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const Key('request_submit')));
      await tester.pumpAndSettle();

      // Assert
      expect(find.byKey(const Key('request_attachment_name')), findsNothing);
      expect(repository.attachments['adjustment-new'], picker.path);
    });

    testWidgets('Given the person backs out of the camera, When it returns, '
        'Then nothing is attached', (tester) async {
      // Arrange
      picker.path = null;
      await pump(tester);

      // Act
      await tester.tap(find.byKey(const Key('request_attachment_camera')));
      await tester.pumpAndSettle();

      // Assert
      expect(find.byKey(const Key('request_attachment_name')), findsNothing);
      expect(
        find.byKey(const Key('request_attachment_camera')),
        findsOneWidget,
      );
    });

    testWidgets('Given the photo fails to upload, When the request landed, '
        'Then it says the pedido stands', (tester) async {
      // Arrange — telling somebody their pedido failed when it did not has
      // them send it twice
      repository.attachFailure = const NetworkException();
      await pump(tester);

      // Act
      await tester.tap(find.byKey(const Key('request_attachment_camera')));
      await tester.pumpAndSettle();
      await tester.tap(
        find.byKey(const Key('request_justification_FORGOT_TO_PUNCH')),
      );
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const Key('request_submit')));
      await tester.pumpAndSettle();

      // Assert
      expect(repository.asked, hasLength(1));
      expect(find.textContaining('o comprovante não subiu'), findsOneWidget);
    });

    testWidgets('Given the request itself fails, When sent, '
        'Then the form says so and stays open', (tester) async {
      // Arrange
      repository.requestFailure = const NetworkException();
      await pump(tester);

      // Act
      await tester.tap(
        find.byKey(const Key('request_justification_FORGOT_TO_PUNCH')),
      );
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const Key('request_submit')));
      await tester.pumpAndSettle();

      // Assert
      expect(find.byKey(const Key('request_error')), findsOneWidget);
      expect(find.byKey(const Key('request_sent')), findsNothing);
    });
  });
}
