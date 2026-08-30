import 'package:flutter/material.dart';

import '../theme/app_colors.dart';
import '../theme/app_radius.dart';
import '../theme/app_spacing.dart';
import '../theme/app_typography.dart';

/// The application shell: content, and a floating bar with a raised action in
/// the middle.
///
/// The centre is not a tab. It is the one thing the employee opened the app to
/// do, and it sits raised and in the accent colour so it is reachable by thumb
/// without looking. The two tabs either side are where they go afterwards.
class AppTabShell extends StatelessWidget {
  const AppTabShell({
    required this.child,
    required this.currentTab,
    required this.onTabSelected,
    required this.onPrimaryAction,
    required this.primaryIcon,
    super.key,
    this.isPrimaryBusy = false,
  });

  final Widget child;
  final AppTab currentTab;
  final ValueChanged<AppTab> onTabSelected;
  final VoidCallback? onPrimaryAction;
  final IconData primaryIcon;
  final bool isPrimaryBusy;

  /// How far the raised action rises above the bar.
  static const double _lift = 30;
  static const double _barHeight = 67;
  static const double _actionSize = 70;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      body: Stack(
        children: [
          // The bar floats, so content has to clear it — otherwise the last
          // row of a list sits permanently under the action.
          Positioned.fill(
            bottom: _barHeight + _lift + AppSpacing.lg,
            child: child,
          ),
          Positioned(
            left: AppSpacing.lg,
            right: AppSpacing.lg,
            bottom: AppSpacing.lg,
            child: _Bar(
              currentTab: currentTab,
              onTabSelected: onTabSelected,
              onPrimaryAction: onPrimaryAction,
              primaryIcon: primaryIcon,
              isPrimaryBusy: isPrimaryBusy,
            ),
          ),
        ],
      ),
    );
  }
}

/// Which side tab is showing.
enum AppTab {
  employee('Funcionário', Icons.badge_outlined),
  history('Histórico', Icons.history);

  const AppTab(this.label, this.icon);

  final String label;
  final IconData icon;
}

class _Bar extends StatelessWidget {
  const _Bar({
    required this.currentTab,
    required this.onTabSelected,
    required this.onPrimaryAction,
    required this.primaryIcon,
    required this.isPrimaryBusy,
  });

  final AppTab currentTab;
  final ValueChanged<AppTab> onTabSelected;
  final VoidCallback? onPrimaryAction;
  final IconData primaryIcon;
  final bool isPrimaryBusy;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      height: AppTabShell._barHeight + AppTabShell._lift,
      child: Stack(
        alignment: Alignment.bottomCenter,
        clipBehavior: Clip.none,
        children: [
          Container(
            height: AppTabShell._barHeight,
            decoration: BoxDecoration(
              color: AppColors.surface,
              borderRadius: BorderRadius.circular(AppRadius.sm),
              boxShadow: const [
                BoxShadow(color: Color(0x99000000), blurRadius: 46),
              ],
            ),
            child: Row(
              children: [
                Expanded(
                  child: _Tab(
                    tab: AppTab.employee,
                    isSelected: currentTab == AppTab.employee,
                    onTap: () => onTabSelected(AppTab.employee),
                  ),
                ),
                // Room for the raised action, which overlaps this row.
                const SizedBox(width: AppTabShell._actionSize),
                Expanded(
                  child: _Tab(
                    tab: AppTab.history,
                    isSelected: currentTab == AppTab.history,
                    onTap: () => onTabSelected(AppTab.history),
                  ),
                ),
              ],
            ),
          ),
          Positioned(
            bottom: AppTabShell._barHeight - AppTabShell._actionSize / 2,
            child: _PrimaryAction(
              icon: primaryIcon,
              onPressed: onPrimaryAction,
              isBusy: isPrimaryBusy,
            ),
          ),
        ],
      ),
    );
  }
}

class _Tab extends StatelessWidget {
  const _Tab({
    required this.tab,
    required this.isSelected,
    required this.onTap,
  });

  final AppTab tab;
  final bool isSelected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    // Selection is carried by colour **and** weight, never by colour alone.
    final colour = isSelected ? AppColors.primary : AppColors.textPrimary;

    return Semantics(
      button: true,
      selected: isSelected,
      label: tab.label,
      child: ExcludeSemantics(
        child: InkWell(
          onTap: onTap,
          borderRadius: BorderRadius.circular(AppRadius.sm),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(tab.icon, size: 24, color: colour),
              const SizedBox(height: 2),
              Text(
                tab.label,
                style: AppTypography.labelSmall.copyWith(
                  color: colour,
                  fontWeight: isSelected ? FontWeight.w700 : FontWeight.w400,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

/// The raised action in the middle of the bar.
class _PrimaryAction extends StatelessWidget {
  const _PrimaryAction({
    required this.icon,
    required this.onPressed,
    required this.isBusy,
  });

  final IconData icon;
  final VoidCallback? onPressed;
  final bool isBusy;

  bool get _isEnabled => onPressed != null && !isBusy;

  @override
  Widget build(BuildContext context) {
    return Semantics(
      button: true,
      enabled: _isEnabled,
      label: 'Marcar ponto',
      child: ExcludeSemantics(
        child: Material(
          color: _isEnabled
              ? AppColors.primary
              : AppColors.primary.withValues(alpha: 0.45),
          shape: const CircleBorder(),
          elevation: 6,
          shadowColor: const Color(0xB3000000),
          child: InkWell(
            onTap: _isEnabled ? onPressed : null,
            customBorder: const CircleBorder(),
            child: SizedBox.square(
              dimension: AppTabShell._actionSize,
              child: isBusy
                  ? const Padding(
                      padding: EdgeInsets.all(AppSpacing.lg),
                      child: CircularProgressIndicator(
                        strokeWidth: 2.6,
                        color: AppColors.onPrimary,
                      ),
                    )
                  : Icon(icon, size: 34, color: AppColors.onPrimary),
            ),
          ),
        ),
      ),
    );
  }
}
