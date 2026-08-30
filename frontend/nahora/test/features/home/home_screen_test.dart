import 'package:nahora/features/home/presentation/home_screen.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../support/pump_design_system.dart';

void main() {
  group('HomeScreen', () {
    testWidgets(
      'Given the app starts, When home renders, Then title and subtitle are shown',
      (tester) async {
        // Arrange & Act
        await pumpScreen(tester, const HomeScreen());

        // Assert
        expect(find.byKey(const Key('home_title')), findsOneWidget);
        expect(find.byKey(const Key('home_subtitle')), findsOneWidget);
      },
    );

    testWidgets(
      'Given the app starts, When home renders, Then both entry actions are available',
      (tester) async {
        // Arrange & Act
        await pumpScreen(tester, const HomeScreen());

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
        await pumpScreen(tester, const HomeScreen());

        // Assert — an overflow banner in the first screen the user sees would be
        // the worst possible first impression, and it is trivial to catch here.
        expect(tester.takeException(), isNull);
      },
    );
  });
}
