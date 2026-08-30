import 'package:nahora/design_system/theme/app_theme.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shadcn_ui/shadcn_ui.dart';

/// Renders a design system component the way the application does.
///
/// Both themes are in scope because the components are adapters: one reads our
/// tokens through the Material theme, the other through the library's. A test
/// that supplied only one would pass on a widget the application cannot build.
Future<void> pumpComponent(WidgetTester tester, Widget component) async {
  tester.view.physicalSize = const Size(1000, 1000);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.reset);

  await tester.pumpWidget(
    ShadTheme(
      data: AppTheme.shad(),
      child: MaterialApp(
        theme: AppTheme.dark(),
        home: Scaffold(body: Center(child: component)),
      ),
    ),
  );
}

/// Renders a full screen, with the providers and both themes the router gives
/// it in production.
Future<void> pumpScreen(
  WidgetTester tester,
  Widget screen, {
  Size size = const Size(390, 844),
  List<Override> overrides = const [],
}) async {
  tester.view.physicalSize = size;
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.reset);

  await tester.pumpWidget(
    ProviderScope(
      overrides: overrides,
      child: ShadTheme(
        data: AppTheme.shad(),
        child: MaterialApp(theme: AppTheme.dark(), home: screen),
      ),
    ),
  );
  await tester.pumpAndSettle();
}
