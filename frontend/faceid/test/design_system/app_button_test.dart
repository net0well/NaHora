import 'package:faceid/design_system/components/app_button.dart';
import 'package:faceid/design_system/components/app_button_variant.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shadcn_ui/shadcn_ui.dart';

import '../support/pump_design_system.dart';

void main() {
  group('AppButton', () {
    testWidgets('Given a label, When rendered, Then the label is visible', (
      tester,
    ) async {
      // Arrange & Act
      await pumpComponent(
        tester,
        AppButton(
          key: const Key('btn'),
          label: 'Bater ponto',
          onPressed: () {},
        ),
      );

      // Assert
      expect(find.text('Bater ponto'), findsOneWidget);
    });

    testWidgets(
      'Given an enabled button, When tapped, Then onPressed fires once',
      (tester) async {
        // Arrange
        var taps = 0;
        await pumpComponent(
          tester,
          AppButton(
            key: const Key('btn'),
            label: 'Entrar',
            onPressed: () => taps++,
          ),
        );

        // Act
        await tester.tap(find.byKey(const Key('btn')));
        await tester.pumpAndSettle();

        // Assert
        expect(taps, 1);
      },
    );

    testWidgets('Given isLoading, When tapped, Then onPressed does not fire', (
      tester,
    ) async {
      // Arrange — the button guards against the double submit that would create
      // a duplicate punch, so callers do not have to remember to disable it.
      var taps = 0;
      await pumpComponent(
        tester,
        AppButton(
          key: const Key('btn'),
          label: 'Entrar',
          isLoading: true,
          onPressed: () => taps++,
        ),
      );

      // Act
      await tester.tap(find.byKey(const Key('btn')));
      await tester.pump();

      // Assert
      expect(taps, 0);
      expect(find.byType(CircularProgressIndicator), findsOneWidget);
    });

    testWidgets(
      'Given onPressed is null, When rendered, Then the button is disabled',
      (tester) async {
        // Arrange & Act
        await pumpComponent(
          tester,
          const AppButton(key: Key('btn'), label: 'Entrar', onPressed: null),
        );

        // Assert
        final button = tester.widget<ShadButton>(find.byType(ShadButton));
        expect(button.onPressed, isNull);
      },
    );

    testWidgets(
      'Given any variant, When rendered, Then it exposes a semantics label',
      (tester) async {
        // Arrange & Act
        await pumpComponent(
          tester,
          AppButton(
            key: const Key('btn'),
            label: 'Confirmar fraude',
            variant: AppButtonVariant.danger,
            onPressed: () {},
          ),
        );

        // Assert — screen readers must announce the action
        expect(
          find.bySemanticsLabel('Confirmar fraude'),
          findsAtLeastNWidgets(1),
        );
      },
    );

    testWidgets('Given every variant, When rendered, Then each one builds', (
      tester,
    ) async {
      // Arrange — a variant that throws only when someone finally uses it is
      // a bug that ships. This walks all four.
      for (final variant in AppButtonVariant.values) {
        // Act
        await pumpComponent(
          tester,
          AppButton(label: variant.name, variant: variant, onPressed: () {}),
        );

        // Assert
        expect(find.text(variant.name), findsOneWidget);
      }
    });
  });
}
