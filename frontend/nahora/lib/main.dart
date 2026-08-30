import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:shadcn_ui/shadcn_ui.dart';

import 'core/config/app_config.dart';
import 'core/routing/app_router.dart';
import 'design_system/theme/app_theme.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();

  // A production build talking to a plaintext endpoint would send credentials
  // and location data in the clear. Fail at startup instead of shipping it.
  assert(
    AppConfig.isTransportSecure,
    'API_BASE_URL must use HTTPS in a production build.',
  );

  runApp(const ProviderScope(child: NaHoraApp()));
}

/// Built once and kept, because rebuilding the router would reset navigation
/// every time the session changes — which is exactly when it must not.
final _routerProvider = Provider<GoRouter>(AppRouter.create);

class NaHoraApp extends ConsumerWidget {
  const NaHoraApp({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    // Both themes are in scope: our components are adapters over the library,
    // and the screens they sit on are still Material. Wrapping the Material app
    // rather than replacing it is what lets the two coexist during the
    // migration instead of forcing it to happen all at once.
    return ShadApp.custom(
      theme: AppTheme.shad(),
      appBuilder: (context) => MaterialApp.router(
        title: 'NaHora',
        debugShowCheckedModeBanner: false,
        theme: AppTheme.light(),
        routerConfig: ref.watch(_routerProvider),
        builder: (context, child) => ShadAppBuilder(child: child!),
      ),
    );
  }
}
