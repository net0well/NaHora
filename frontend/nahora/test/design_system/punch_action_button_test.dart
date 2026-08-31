import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:nahora/design_system/components/punch_action_button.dart';

import '../support/pump_design_system.dart';

/// The control the whole product exists for.
void main() {
  group('PunchActionButton', () {
    testWidgets('Given a time, When shown, Then the clock face is drawn', (
      tester,
    ) async {
      // Arrange & Act
      await pumpComponent(
        tester,
        PunchActionButton(time: DateTime(2026, 8, 30, 8, 5), onPressed: () {}),
      );

      // Assert
      expect(find.byKey(const Key('punch_action_clock')), findsOneWidget);
    });

    testWidgets(
      'Given no synced time, When shown, Then it still draws rather than blanking',
      (tester) async {
        // Arrange & Act — the face shows its ticks and no hands. Falling back
        // to the device clock is the one thing this product must never do.
        await pumpComponent(
          tester,
          PunchActionButton(time: null, onPressed: () {}),
        );

        // Assert
        expect(find.byKey(const Key('punch_action_clock')), findsOneWidget);
      },
    );

    testWidgets('Given the button, When tapped, Then it fires once', (
      tester,
    ) async {
      // Arrange
      var taps = 0;

      await pumpComponent(
        tester,
        PunchActionButton(
          time: DateTime(2026, 8, 30, 8, 5),
          onPressed: () => taps++,
        ),
      );

      // Act
      await tester.tap(find.byKey(const Key('punch_action_clock')));
      await tester.pumpAndSettle();

      // Assert
      expect(taps, 1);
    });

    testWidgets(
      'Given no action, When shown, Then it is announced as disabled',
      (tester) async {
        // Arrange & Act
        await pumpComponent(
          tester,
          const PunchActionButton(time: null, onPressed: null),
        );

        // Assert
        expect(
          tester.getSemantics(find.bySemanticsLabel('Marcar ponto')),
          matchesSemantics(
            label: 'Marcar ponto',
            isButton: true,
            hasEnabledState: true,
            isEnabled: false,
          ),
        );
      },
    );

    testWidgets(
      'Given it is busy, When shown, Then the clock gives way to progress',
      (tester) async {
        // Arrange & Act — the hands would keep ticking under a request that is
        // already in flight, which reads as nothing having happened
        await pumpComponent(
          tester,
          PunchActionButton(
            time: DateTime(2026, 8, 30, 8, 5),
            onPressed: () {},
            isBusy: true,
          ),
        );

        // Assert
        expect(find.byKey(const Key('punch_action_clock')), findsNothing);
        expect(find.byType(CircularProgressIndicator), findsOneWidget);
      },
    );

    testWidgets(
      'Given reduce motion is off, When shown, Then the pulse animates',
      (tester) async {
        // Arrange — pumped by hand rather than settled: the pulse repeats
        // forever on purpose, and pumpAndSettle would wait for an end that
        // never comes
        tester.view.physicalSize = const Size(400, 400);
        tester.view.devicePixelRatio = 1.0;
        addTearDown(tester.view.reset);

        await tester.pumpWidget(
          MaterialApp(
            home: Scaffold(
              body: Center(
                child: PunchActionButton(
                  time: DateTime(2026, 8, 30, 8, 5),
                  onPressed: () {},
                ),
              ),
            ),
          ),
        );

        // Act
        await tester.pump(const Duration(milliseconds: 500));

        // Assert — still on screen and still ticking, which is all the test can
        // say about a painter without reading pixels
        expect(find.byType(PunchActionButton), findsOneWidget);
        expect(tester.hasRunningAnimations, isTrue);

        // Left running would fail the test with a pending timer.
        await tester.pumpWidget(const SizedBox.shrink());
      },
    );
  });
}
