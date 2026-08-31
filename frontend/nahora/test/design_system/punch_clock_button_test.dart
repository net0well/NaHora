import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:nahora/design_system/components/punch_clock_button.dart';

import '../support/pump_design_system.dart';

/// Holding to record a punch.
///
/// A punch cannot be undone — the row is append-only and a correction costs a
/// manager's time. The hold is what separates deciding from brushing the screen
/// with a glove.
void main() {
  group('PunchClockButton', () {
    const hold = Duration(milliseconds: 900);

    Future<void> pump(
      WidgetTester tester, {
      required VoidCallback? onPressed,
      bool isLoading = false,
    }) => pumpComponent(
      tester,
      PunchClockButton(
        dateLabel: 'Dom, 30 Ago 2026',
        timeLabel: '17:35:20',
        onPressed: onPressed,
        isLoading: isLoading,
        holdDuration: hold,
      ),
    );

    testWidgets(
      'Given the button, When shown, Then it says what it does and how',
      (tester) async {
        // Arrange & Act — an amber rectangle with a clock is not self-evidently
        // the thing that records a punch
        await pump(tester, onPressed: () {});

        // Assert
        expect(find.text('Segure para marcar ponto'), findsOneWidget);
        expect(find.text('17:35:20'), findsOneWidget);
      },
    );

    testWidgets(
      'Given a quick tap, When released early, Then nothing is recorded',
      (tester) async {
        // Arrange — the whole point: a brushed screen must not become a legal
        // record
        var punches = 0;
        await pump(tester, onPressed: () => punches++);

        // Act
        final gesture = await tester.startGesture(
          tester.getCenter(find.byType(PunchClockButton)),
        );
        await tester.pump(const Duration(milliseconds: 200));
        await gesture.up();
        await tester.pumpAndSettle();

        // Assert
        expect(punches, 0);
      },
    );

    testWidgets(
      'Given the hold is completed, When it fills, Then the punch is recorded',
      (tester) async {
        // Arrange
        var punches = 0;
        await pump(tester, onPressed: () => punches++);

        // Act
        final gesture = await tester.startGesture(
          tester.getCenter(find.byType(PunchClockButton)),
        );

        // A frame first, so the gesture arena resolves and onTapDown fires;
        // only then does advancing the clock move the ring.
        await tester.pump();
        await tester.pump(hold + const Duration(milliseconds: 50));

        // Assert
        expect(punches, 1);

        await gesture.up();
        await tester.pumpAndSettle();
      },
    );

    testWidgets(
      'Given a hold in progress, When it is measured, Then the ring shows how far',
      (tester) async {
        // Arrange
        await pump(tester, onPressed: () {});

        // Act — halfway through
        final gesture = await tester.startGesture(
          tester.getCenter(find.byType(PunchClockButton)),
        );
        await tester.pump();
        await tester.pump(const Duration(milliseconds: 450));

        // Assert
        final ring = tester.widget<CircularProgressIndicator>(
          find.byKey(const Key('punch_button_hold')),
        );
        expect(ring.value, greaterThan(0));
        expect(ring.value, lessThan(1));

        await gesture.up();
        await tester.pumpAndSettle();
      },
    );

    testWidgets(
      'Given the request is in flight, When shown, Then the ring stops measuring',
      (tester) async {
        // Arrange & Act — the hold is over and the wait belongs to the server
        await pump(tester, onPressed: () {}, isLoading: true);

        // Assert
        expect(find.byKey(const Key('punch_button_sending')), findsOneWidget);
        expect(find.byKey(const Key('punch_button_hold')), findsNothing);
        expect(find.text('Registrando...'), findsOneWidget);
      },
    );

    testWidgets(
      'Given no action, When held, Then nothing happens and it reads disabled',
      (tester) async {
        // Arrange & Act
        await pump(tester, onPressed: null);

        final gesture = await tester.startGesture(
          tester.getCenter(find.byType(PunchClockButton)),
        );
        await tester.pump();
        await tester.pump(hold + const Duration(milliseconds: 50));
        await gesture.up();
        await tester.pumpAndSettle();

        // Assert
        final ring = tester.widget<CircularProgressIndicator>(
          find.byKey(const Key('punch_button_hold')),
        );
        expect(ring.value, 0);
      },
    );

    testWidgets(
      'Given a screen reader, When it activates the button, Then it fires at once',
      (tester) async {
        // Arrange — somebody who cannot see the ring cannot hold to fill it,
        // and the hold guards against a brushed screen, which is not how this
        // control is reached
        await pump(tester, onPressed: () {});
        final handle = tester.ensureSemantics();

        // Act & Assert — the node carries a plain tap action, so assistive
        // technology activates it without a timed gesture
        expect(
          tester.getSemantics(find.bySemanticsLabel(RegExp('Marcar ponto'))),
          matchesSemantics(
            label: 'Marcar ponto. Dom, 30 Ago 2026, 17:35:20',
            isButton: true,
            hasEnabledState: true,
            isEnabled: true,
            hasTapAction: true,
          ),
        );

        handle.dispose();
      },
    );
  });
}
