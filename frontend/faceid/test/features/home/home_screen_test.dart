import 'package:faceid/design_system/theme/app_theme.dart';
import 'package:faceid/features/home/presentation/home_screen.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('HomeScreen', () {
    Future<void> pumpScreen(WidgetTester tester) async {
      tester.view.physicalSize = const Size(1280, 720);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(tester.view.reset);

      await tester.pumpWidget(
        ProviderScope(
          child: MaterialApp(theme: AppTheme.light(), home: const HomeScreen()),
        ),
      );
      await tester.pumpAndSettle();
    }

    testWidgets(
      'Given the app starts, When home renders, Then title and subtitle are shown',
      (tester) async {
        // Arrange & Act
        await pumpScreen(tester);

        // Assert
        expect(find.byKey(const Key('home_title')), findsOneWidget);
        expect(find.byKey(const Key('home_subtitle')), findsOneWidget);
      },
    );

    testWidgets(
      'Given the app starts, When home renders, Then both entry actions are available',
      (tester) async {
        // Arrange & Act
        await pumpScreen(tester);

        // Assert
        expect(find.byKey(const Key('home_login_button')), findsOneWidget);
        expect(
          find.byKey(const Key('home_register_company_button')),
          findsOneWidget,
        );
      },
    );

    testWidgets(
      'Given a 1280x720 window, When home renders, Then nothing overflows',
      (tester) async {
        // Arrange & Act
        await pumpScreen(tester);

        // Assert — an overflow banner in the first screen the user sees would be
        // the worst possible first impression, and it is trivial to catch here.
        expect(tester.takeException(), isNull);
      },
    );
  });
}
