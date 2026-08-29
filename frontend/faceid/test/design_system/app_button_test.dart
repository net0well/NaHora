import 'package:faceid/design_system/components/app_button.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('AppButton', () {
    Widget wrap(Widget child) => MaterialApp(home: Scaffold(body: Center(child: child)));

    testWidgets('Given a label, When rendered, Then the label is visible', (tester) async {
      // Arrange
      tester.view.physicalSize = const Size(1000, 1000);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(tester.view.reset);

      // Act
      await tester.pumpWidget(wrap(
        AppButton(key: const Key('btn'), label: 'Bater ponto', onPressed: () {}),
      ));

      // Assert
      expect(find.text('Bater ponto'), findsOneWidget);
    });

    testWidgets('Given an enabled button, When tapped, Then onPressed fires once', (tester) async {
      // Arrange
      tester.view.physicalSize = const Size(1000, 1000);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(tester.view.reset);

      var taps = 0;
      await tester.pumpWidget(wrap(
        AppButton(key: const Key('btn'), label: 'Entrar', onPressed: () => taps++),
      ));

      // Act
      await tester.tap(find.byKey(const Key('btn')));
      await tester.pumpAndSettle();

      // Assert
      expect(taps, 1);
    });

    testWidgets('Given isLoading, When tapped, Then onPressed does not fire', (tester) async {
      // Arrange — the button guards against the double submit that would create
      // a duplicate punch, so callers do not have to remember to disable it.
      tester.view.physicalSize = const Size(1000, 1000);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(tester.view.reset);

      var taps = 0;
      await tester.pumpWidget(wrap(
        AppButton(
          key: const Key('btn'),
          label: 'Entrar',
          isLoading: true,
          onPressed: () => taps++,
        ),
      ));

      // Act
      await tester.tap(find.byKey(const Key('btn')));
      await tester.pump();

      // Assert
      expect(taps, 0);
      expect(find.byType(CircularProgressIndicator), findsOneWidget);
    });

    testWidgets('Given onPressed is null, When rendered, Then the button is disabled', (tester) async {
      // Arrange
      tester.view.physicalSize = const Size(1000, 1000);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(tester.view.reset);

      // Act
      await tester.pumpWidget(wrap(
        const AppButton(key: Key('btn'), label: 'Entrar', onPressed: null),
      ));

      // Assert
      final button = tester.widget<FilledButton>(find.byType(FilledButton));
      expect(button.onPressed, isNull);
    });

    testWidgets('Given any variant, When rendered, Then it exposes a semantics label', (tester) async {
      // Arrange
      tester.view.physicalSize = const Size(1000, 1000);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(tester.view.reset);

      // Act
      await tester.pumpWidget(wrap(
        AppButton(
          key: const Key('btn'),
          label: 'Confirmar fraude',
          variant: AppButtonVariant.danger,
          onPressed: () {},
        ),
      ));

      // Assert — screen readers must announce the action
      expect(
        find.bySemanticsLabel('Confirmar fraude'),
        findsAtLeastNWidgets(1),
      );
    });
  });
}
