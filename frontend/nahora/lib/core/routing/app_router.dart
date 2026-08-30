import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../features/auth/presentation/auth_providers.dart';
import '../../features/auth/presentation/auth_state.dart';
import '../../features/attendance/presentation/punch_screen.dart';
import '../../features/auth/presentation/login_screen.dart';
import '../../features/home/presentation/home_screen.dart';

abstract final class AppRoutes {
  static const String splash = '/';
  static const String login = '/login';
  static const String dashboard = '/dashboard';
  static const String attendance = '/attendance';
}

/// Router for the application shell.
///
/// Navigation decisions live here rather than scattered across widgets, so
/// there is one place to read when the question is "how did the user get to
/// this screen".
abstract final class AppRouter {
  static GoRouter create(Ref ref) {
    return GoRouter(
      initialLocation: AppRoutes.splash,
      refreshListenable: _AuthListenable(ref),
      redirect: (context, state) => _redirect(ref, state),
      routes: [
        GoRoute(
          path: AppRoutes.splash,
          name: 'splash',
          builder: (context, state) => const _SplashScreen(),
        ),
        GoRoute(
          path: AppRoutes.login,
          name: 'login',
          builder: (context, state) => LoginScreen(
            companyId: state.uri.queryParameters['company'] ?? '',
          ),
        ),
        GoRoute(
          path: AppRoutes.dashboard,
          name: 'dashboard',
          builder: (context, state) => const HomeScreen(),
        ),
        GoRoute(
          path: AppRoutes.attendance,
          name: 'attendance',
          builder: (context, state) => const PunchScreen(),
        ),
      ],
    );
  }

  /// Decides where the user belongs, from the session alone.
  static String? _redirect(Ref ref, GoRouterState state) {
    final auth = ref.read(authControllerProvider);
    final location = state.matchedLocation;

    return switch (auth) {
      // Nowhere, yet. Sending anyone anywhere while the stored token is still
      // being read would bounce a signed-in user to the login screen for the
      // instant it takes to answer.
      AuthChecking() => location == AppRoutes.splash ? null : AppRoutes.splash,

      AuthSignedOut() => location == AppRoutes.login ? null : AppRoutes.login,

      // Straight to the punch screen: it is what the employee opened the app
      // for, and a dashboard in between would be a tap between them and the
      // one thing they came to do.
      AuthSignedIn() =>
        location == AppRoutes.login || location == AppRoutes.splash
            ? AppRoutes.attendance
            : null,
    };
  }
}

/// Bridges Riverpod to GoRouter's refresh mechanism.
///
/// GoRouter re-evaluates the redirect when this notifies. Without it the guard
/// runs only on navigation, and a session that dies mid-screen would leave the
/// user looking at a page whose requests all fail.
class _AuthListenable extends ChangeNotifier {
  _AuthListenable(Ref ref) {
    ref.listen(authControllerProvider, (_, _) => notifyListeners());
  }
}

/// Shown only while the stored session is being read.
class _SplashScreen extends StatelessWidget {
  const _SplashScreen();

  @override
  Widget build(BuildContext context) {
    return const Scaffold(body: Center(child: CircularProgressIndicator()));
  }
}
