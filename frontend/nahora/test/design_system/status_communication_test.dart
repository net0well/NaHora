import 'package:nahora/design_system/components/app_badge.dart';
import 'package:nahora/design_system/components/app_badge_tone.dart';
import 'package:nahora/design_system/components/status_indicator.dart';
import 'package:nahora/design_system/components/status_tone.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import '../support/pump_design_system.dart';

/// Spec 010 requires every state to be communicated by colour **and** icon
/// **and** text. These check the two components whose whole job is reporting
/// state, because that is where the rule matters most: roughly 8% of men have
/// some colour vision deficiency, and a component that distinguishes approved
/// from rejected by hue alone tells them nothing.
void main() {
  group('AppBadge', () {
    testWidgets(
      'Given any tone, When rendered, Then it shows an icon beside the text',
      (tester) async {
        for (final tone in AppBadgeTone.values) {
          // Arrange & Act
          await pumpComponent(
            tester,
            AppBadge(label: 'Estado ${tone.name}', tone: tone),
          );

          // Assert
          expect(find.text('Estado ${tone.name}'), findsOneWidget);
          expect(
            find.byType(Icon),
            findsOneWidget,
            reason: 'tone ${tone.name} would be colour-only without an icon',
          );
        }
      },
    );

    testWidgets(
      'Given two different tones, When rendered, Then their icons differ',
      (tester) async {
        // Arrange — identical icons would put the distinction back on colour
        await pumpComponent(
          tester,
          const AppBadge(label: 'ok', tone: AppBadgeTone.success),
        );
        final success = tester.widget<Icon>(find.byType(Icon)).icon;

        // Act
        await pumpComponent(
          tester,
          const AppBadge(label: 'erro', tone: AppBadgeTone.danger),
        );
        final danger = tester.widget<Icon>(find.byType(Icon)).icon;

        // Assert
        expect(success, isNot(danger));
      },
    );
  });

  group('StatusIndicator', () {
    testWidgets(
      'Given any tone, When rendered, Then colour, icon and text are all present',
      (tester) async {
        for (final tone in StatusTone.values) {
          // Arrange & Act
          await pumpComponent(
            tester,
            StatusIndicator(label: 'Situação ${tone.name}', tone: tone),
          );

          // Assert
          final icon = tester.widget<Icon>(find.byType(Icon));

          expect(find.text('Situação ${tone.name}'), findsOneWidget);
          expect(icon.color, isNotNull);
        }
      },
    );

    testWidgets(
      'Given a status change, When rendered, Then it is announced as a live region',
      (tester) async {
        // Arrange — the camera frame reports state through this. A screen
        // reader user who is not told the state changed is stuck.
        await pumpComponent(
          tester,
          const StatusIndicator(
            label: 'Ponto registrado',
            tone: StatusTone.success,
          ),
        );

        // Act
        final semantics = tester.getSemantics(
          find.bySemanticsLabel('Ponto registrado'),
        );

        // Assert
        expect(semantics.flagsCollection.isLiveRegion, isTrue);
      },
    );
  });
}
