import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:nahora/design_system/components/app_segmented_control.dart';

import '../support/pump_design_system.dart';

/// Switching between two views of one screen.
void main() {
  group('AppSegmentedControl', () {
    Future<void> pump(
      WidgetTester tester, {
      required String selectedId,
      required ValueChanged<String> onChanged,
      String? badge,
    }) => pumpComponent(
      tester,
      AppSegmentedControl(
        selectedId: selectedId,
        onChanged: onChanged,
        segments: [
          const AppSegment(id: 'profile', label: 'Meus dados'),
          AppSegment(id: 'adjustments', label: 'Ajustes', badge: badge),
        ],
      ),
    );

    testWidgets('Given both options, When shown, '
        'Then neither is hidden behind a menu', (tester) async {
      // Arrange & Act — the point of the control is that the other view is
      // visible without opening anything
      await pump(tester, selectedId: 'profile', onChanged: (_) {});

      // Assert
      expect(find.text('Meus dados'), findsOneWidget);
      expect(find.text('Ajustes'), findsOneWidget);
    });

    testWidgets(
      'Given the other segment, When tapped, Then the choice is reported',
      (tester) async {
        // Arrange
        String? chosen;
        await pump(
          tester,
          selectedId: 'profile',
          onChanged: (id) => chosen = id,
        );

        // Act
        await tester.tap(find.byKey(const Key('segment_adjustments')));
        await tester.pumpAndSettle();

        // Assert
        expect(chosen, 'adjustments');
      },
    );

    testWidgets('Given the selected segment, When tapped again, '
        'Then it still reports rather than toggling', (tester) async {
      // Arrange — the control owns no state; a parent that rebuilt on its own
      // guess would drift from what is actually rendered
      String? chosen;
      await pump(tester, selectedId: 'profile', onChanged: (id) => chosen = id);

      // Act
      await tester.tap(find.byKey(const Key('segment_profile')));
      await tester.pumpAndSettle();

      // Assert
      expect(chosen, 'profile');
    });

    testWidgets('Given a count, When shown, Then it rides inside its segment', (
      tester,
    ) async {
      // Arrange & Act
      await pump(tester, selectedId: 'profile', onChanged: (_) {}, badge: '3');

      // Assert
      expect(find.text('3'), findsOneWidget);
    });

    testWidgets('Given a screen reader, When it reads the control, '
        'Then which one is selected is spoken, not only coloured', (
      tester,
    ) async {
      // Arrange — the selected segment is distinguished by fill; that says
      // nothing to somebody who cannot see it
      final handle = tester.ensureSemantics();
      await pump(tester, selectedId: 'profile', onChanged: (_) {});

      // Act & Assert
      expect(
        tester.getSemantics(find.bySemanticsLabel('Meus dados')),
        matchesSemantics(
          label: 'Meus dados',
          isButton: true,
          isSelected: true,
          hasSelectedState: true,
          hasTapAction: true,
        ),
      );

      handle.dispose();
    });
  });
}
