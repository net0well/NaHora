import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/routing/app_router.dart';
import '../../../design_system/components/app_tab_shell.dart';
import '../../attendance/presentation/attendance_providers.dart';
import '../../attendance/presentation/server_clock_controller.dart';
import 'employee_tab.dart';
import 'history_tab.dart';

/// The app's home: two tabs with the punch action raised between them.
///
/// The action navigates rather than punching in place. Marking a point needs
/// the location on screen first — the employee should see what is about to be
/// recorded before it is, not after.
class HomeShellScreen extends ConsumerStatefulWidget {
  const HomeShellScreen({super.key});

  @override
  ConsumerState<HomeShellScreen> createState() => _HomeShellScreenState();
}

class _HomeShellScreenState extends ConsumerState<HomeShellScreen> {
  AppTab _tab = AppTab.employee;
  Timer? _ticker;

  @override
  void initState() {
    super.initState();

    // Repaints the clock face once a second. The clock itself advances from its
    // monotonic anchor whether or not anything is watching; this only moves the
    // hands.
    _ticker = Timer.periodic(const Duration(seconds: 1), (_) {
      if (mounted) {
        setState(() {});
      }
    });
  }

  @override
  void dispose() {
    _ticker?.cancel();
    super.dispose();
  }

  /// Opens the punch screen and reloads what it may have changed.
  ///
  /// The refresh is explicit rather than left to auto-disposal: the punch
  /// screen is pushed on top of this one, so the tab underneath stays mounted
  /// and its providers are never dropped. Without this the employee returns
  /// from a confirmed punch to a list that does not contain it.
  Future<void> _punchThenRefresh() async {
    await context.push(AppRoutes.attendance);

    if (!mounted) {
      return;
    }

    ref
      ..invalidate(punchDayProvider)
      ..invalidate(punchHistoryProvider);
  }

  /// The official time in company hours, or null until the clock has synced.
  ///
  /// Never the device clock. A phone running two hours fast would put the wrong
  /// time on the button that records the right one.
  DateTime? _officialTime() {
    final utc = ref.read(serverClockProvider).nowUtc();
    if (utc == null) {
      return null;
    }

    return utc.add(ref.read(serverClockControllerProvider.notifier).offset);
  }

  @override
  Widget build(BuildContext context) {
    // Watched so the clock synchronises as soon as the shell appears, rather
    // than only when somebody opens the punch screen.
    ref.watch(serverClockControllerProvider);

    return AppTabShell(
      currentTab: _tab,
      onTabSelected: (tab) => setState(() => _tab = tab),
      onPrimaryAction: _punchThenRefresh,

      // A live clock face, not a fingerprint. The product confirms by location
      // and reads no biometrics; a fingerprint would promise something it does
      // not deliver, to the person least able to check.
      primaryTime: _officialTime(),
      child: switch (_tab) {
        AppTab.employee => const EmployeeTab(),
        AppTab.history => const HistoryTab(),
      },
    );
  }
}
