import 'package:flutter_test/flutter_test.dart';
import 'package:nahora/design_system/components/app_location_preview.dart';

import '../support/pump_design_system.dart';

/// What the preview is allowed to claim about the location.
void main() {
  group('AppLocationPreview', () {
    testWidgets(
      'Given an accurate fix, When shown, Then it confirms with the reading',
      (tester) async {
        // Arrange & Act
        await pumpComponent(
          tester,
          const AppLocationPreview(accuracyMeters: 12, height: 190),
        );

        // Assert
        expect(find.text('Localização confirmada · 12 m'), findsOneWidget);
      },
    );

    testWidgets(
      'Given a degraded fix, When shown, Then it says so and still shows the reading',
      (tester) async {
        // Arrange & Act — the punch is recorded either way; this only explains
        // what the record proves
        await pumpComponent(
          tester,
          const AppLocationPreview(
            accuracyMeters: 480,
            height: 190,
            isDegraded: true,
          ),
        );

        // Assert
        expect(find.text('Precisão baixa · 480 m'), findsOneWidget);
      },
    );

    testWidgets('Given no reading at all, When shown, '
        'Then it reports no signal rather than confirming nothing', (
      tester,
    ) async {
      // Arrange & Act — the bug this pins: with no accuracy the success
      // branch rendered "confirmada · null m", directly contradicting the
      // error the screen showed underneath it
      await pumpComponent(
        tester,
        const AppLocationPreview(accuracyMeters: null, height: 190),
      );

      // Assert
      expect(find.text('Sem sinal de GPS'), findsOneWidget);
      expect(find.textContaining('null'), findsNothing);
      expect(find.textContaining('confirmada'), findsNothing);
    });

    testWidgets(
      'Given no reading but the degraded flag, When shown, Then still no signal',
      (tester) async {
        // Arrange & Act — a flag cannot conjure a measurement
        await pumpComponent(
          tester,
          const AppLocationPreview(
            accuracyMeters: null,
            height: 190,
            isDegraded: true,
          ),
        );

        // Assert
        expect(find.text('Sem sinal de GPS'), findsOneWidget);
        expect(find.textContaining('null'), findsNothing);
      },
    );
  });
}
