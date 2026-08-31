import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:nahora/design_system/components/app_location_preview.dart';
import 'package:nahora/design_system/components/app_map.dart';

import '../support/pump_design_system.dart';

/// Showing where a mark was made.
///
/// The cases that matter are the ones where there is no map: a build with no
/// key, and a device with no fix. Neither may look like the punch is at risk,
/// because the coordinate is captured and sent either way.
void main() {
  group('AppMap', () {
    Future<void> pump(
      WidgetTester tester, {
      String tileKey = 'test-key',
      double? latitude = -23.5505,
      double? longitude = -46.6333,
      double? accuracyMeters = 12,
      bool isUnavailable = false,
      bool isDegraded = false,
      bool isInteractive = false,
    }) => pumpComponent(
      tester,
      AppMap(
        height: 200,
        tileKey: tileKey,
        latitude: latitude,
        longitude: longitude,
        accuracyMeters: accuracyMeters,
        isUnavailable: isUnavailable,
        isDegraded: isDegraded,
        isInteractive: isInteractive,
      ),
    );

    testWidgets('Given a key and a fix, When shown, Then the pin is on a map', (
      tester,
    ) async {
      // Arrange & Act
      await pump(tester);

      // Assert
      expect(find.byKey(const Key('app_map')), findsOneWidget);
      expect(find.byKey(const Key('app_map_marker')), findsOneWidget);
    });

    testWidgets('Given no key, When shown, '
        'Then it states the position instead of failing', (tester) async {
      // Arrange & Act — a build without a map key is a supported build; the
      // punch is recorded with the same coordinate either way
      await pump(tester, tileKey: '');

      // Assert
      expect(find.byType(AppLocationPreview), findsOneWidget);
      expect(find.byKey(const Key('app_map')), findsNothing);
      expect(find.textContaining('Localização confirmada'), findsOneWidget);
    });

    testWidgets('Given no fix, When shown, Then no pin is dropped anywhere', (
      tester,
    ) async {
      // Arrange & Act — a map centred on nothing would still draw a pin, and a
      // pin somewhere wrong is worse than no map
      await pump(tester, latitude: null, longitude: null);

      // Assert
      expect(find.byType(AppLocationPreview), findsOneWidget);
      expect(find.byKey(const Key('app_map_marker')), findsNothing);
    });

    testWidgets('Given the GPS is unavailable, When shown, '
        'Then it says so rather than mapping a stale point', (tester) async {
      // Arrange & Act
      await pump(tester, isUnavailable: true);

      // Assert
      expect(find.text('Sem sinal de GPS'), findsOneWidget);
      expect(find.byKey(const Key('app_map')), findsNothing);
    });

    testWidgets('Given tiles are drawn, When shown, '
        'Then the provider is credited', (tester) async {
      // Arrange & Act — required by the tile provider's terms and by the
      // licence of the data underneath them
      await pump(tester);

      // Assert
      expect(find.text('© MapTiler © OpenStreetMap'), findsOneWidget);
    });

    testWidgets(
      'Given a fix, When shown, Then the radius is drawn, not implied',
      (tester) async {
        // Arrange & Act — a pin with no circle claims a precision the GPS never
        // reported
        await pump(tester, accuracyMeters: 45);

        // Assert
        final circles = tester.widget<CircleLayer>(find.byType(CircleLayer));
        final circle = circles.circles.single;
        expect(circle.radius, 45);
        expect(circle.useRadiusInMeter, isTrue);
      },
    );

    testWidgets('Given a map at rest, When shown, '
        'Then no way back is offered yet', (tester) async {
      // Arrange & Act — a control that is always there is one more thing to
      // read on a screen whose job is a single decision
      await pump(tester, isInteractive: true);

      // Assert
      expect(find.byKey(const Key('app_map_recentre')), findsNothing);
    });

    testWidgets('Given the map is dragged away, When it settles, '
        'Then the way back appears and the mark has not moved', (tester) async {
      // Arrange
      await pump(tester, isInteractive: true);
      final marker = tester.getCenter(find.byKey(const Key('app_map_marker')));

      // Act
      await tester.drag(
        find.byKey(const Key('app_map')),
        const Offset(-260, 0),
      );
      await tester.pumpAndSettle();

      // Assert
      expect(find.byKey(const Key('app_map_recentre')), findsOneWidget);

      // The pin travelled with the map rather than staying under the middle of
      // the screen: it is bound to the coordinate, not to the view.
      expect(
        tester.getCenter(find.byKey(const Key('app_map_marker'))).dx,
        lessThan(marker.dx),
      );
    });

    testWidgets('Given the map was moved, When the way back is used, '
        'Then the view returns and the control goes', (tester) async {
      // Arrange
      await pump(tester, isInteractive: true);
      await tester.drag(
        find.byKey(const Key('app_map')),
        const Offset(-260, 0),
      );
      await tester.pumpAndSettle();

      // Act
      await tester.tap(find.byKey(const Key('app_map_recentre')));
      await tester.pumpAndSettle();

      // Assert
      expect(find.byKey(const Key('app_map_recentre')), findsNothing);
    });

    testWidgets('Given a map that cannot be moved, When dragged, '
        'Then nothing happens', (tester) async {
      // Arrange & Act
      await pump(tester);
      await tester.drag(
        find.byKey(const Key('app_map')),
        const Offset(-260, 0),
      );
      await tester.pumpAndSettle();

      // Assert
      expect(find.byKey(const Key('app_map_recentre')), findsNothing);
    });

    testWidgets('Given the first fix arrives, When it replaces the fallback, '
        'Then the map opens on it without being driven', (tester) async {
      // Arrange — the punch screen starts with no fix and shows the card. The
      // controller cannot be used before a map has rendered, and reaching for
      // it here threw where the whole screen could see it.
      await pump(tester, latitude: null, longitude: null, isInteractive: true);

      // Act
      await pump(tester, isInteractive: true);

      // Assert
      expect(tester.takeException(), isNull);
      expect(find.byKey(const Key('app_map_marker')), findsOneWidget);
    });

    testWidgets('Given a screen reader, When it reaches the map, '
        'Then the precision is spoken', (tester) async {
      // Arrange — a map says nothing to somebody who cannot see it, but the
      // number underneath it is the part that matters
      final handle = tester.ensureSemantics();
      await pump(tester, accuracyMeters: 12);

      // Act & Assert
      expect(
        find.bySemanticsLabel('Mapa da localização, precisão de 12 metros'),
        findsOneWidget,
      );

      handle.dispose();
    });
  });
}
