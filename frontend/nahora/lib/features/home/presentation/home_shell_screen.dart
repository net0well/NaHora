import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/routing/app_router.dart';
import '../../../design_system/components/app_tab_shell.dart';
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

  @override
  Widget build(BuildContext context) {
    return AppTabShell(
      currentTab: _tab,
      onTabSelected: (tab) => setState(() => _tab = tab),
      onPrimaryAction: () => context.push(AppRoutes.attendance),

      // A clock, not a fingerprint. The product confirms by location and reads
      // no biometrics; an icon suggesting otherwise would promise something it
      // does not deliver, to the person least able to check.
      primaryIcon: Icons.schedule,
      child: switch (_tab) {
        AppTab.employee => const EmployeeTab(),
        AppTab.history => const HistoryTab(),
      },
    );
  }
}
