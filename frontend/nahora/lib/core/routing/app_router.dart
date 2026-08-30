import 'package:go_router/go_router.dart';

import '../../features/home/presentation/home_screen.dart';

abstract final class AppRoutes {
  static const String home = '/';
  static const String login = '/login';
  static const String dashboard = '/dashboard';
  static const String attendance = '/attendance';
}

/// Router for the application shell.
///
/// Auth and onboarding guards attach through `redirect` once the session
/// provider exists; the routes are declared here so navigation is never
/// scattered across widgets.
abstract final class AppRouter {
  static GoRouter create() => GoRouter(
    initialLocation: AppRoutes.home,
    routes: [
      GoRoute(
        path: AppRoutes.home,
        name: 'home',
        builder: (context, state) => const HomeScreen(),
      ),
    ],
  );
}
