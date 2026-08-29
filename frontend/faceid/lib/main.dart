import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'core/config/app_config.dart';
import 'core/routing/app_router.dart';
import 'design_system/theme/app_theme.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();

  // A production build talking to a plaintext endpoint would send credentials
  // and biometric frames in the clear. Fail at startup instead of shipping it.
  assert(
    AppConfig.isTransportSecure,
    'API_BASE_URL must use HTTPS in a production build.',
  );

  runApp(const ProviderScope(child: FaceIdApp()));
}

class FaceIdApp extends StatelessWidget {
  const FaceIdApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp.router(
      title: 'FaceId',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.light(),
      routerConfig: AppRouter.create(),
    );
  }
}
