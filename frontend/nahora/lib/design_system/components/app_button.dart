import 'package:flutter/material.dart';
import 'package:shadcn_ui/shadcn_ui.dart';

import '../theme/app_colors.dart';
import '../theme/app_sizing.dart';
import 'app_button_variant.dart';

/// Application button.
///
/// A thin adapter over the component library, which is what makes ADR-006
/// reversible: replacing the library rewrites this file, not the screens.
///
/// It owns its loading state so callers never have to remember to disable the
/// button while a request is in flight — the double submit that creates a
/// duplicate punch is not left to discipline.
class AppButton extends StatelessWidget {
  const AppButton({
    required this.label,
    required this.onPressed,
    super.key,
    this.variant = AppButtonVariant.primary,
    this.icon,
    this.isLoading = false,
    this.expanded = false,
  });

  final String label;
  final VoidCallback? onPressed;
  final AppButtonVariant variant;
  final IconData? icon;
  final bool isLoading;
  final bool expanded;

  bool get _isEnabled => onPressed != null && !isLoading;

  @override
  Widget build(BuildContext context) {
    final child = Text(label);
    final leading = _buildLeading();
    final width = expanded ? double.infinity : null;
    final onTap = _isEnabled ? onPressed : null;

    final button = switch (variant) {
      AppButtonVariant.primary => ShadButton(
        onPressed: onTap,
        leading: leading,
        height: AppSizing.buttonHeight,
        width: width,
        child: child,
      ),
      AppButtonVariant.secondary => ShadButton.outline(
        onPressed: onTap,
        leading: leading,
        height: AppSizing.buttonHeight,
        width: width,
        child: child,
      ),
      AppButtonVariant.danger => ShadButton.destructive(
        onPressed: onTap,
        leading: leading,
        height: AppSizing.buttonHeight,
        width: width,
        child: child,
      ),
      AppButtonVariant.text => ShadButton.link(
        onPressed: onTap,
        leading: leading,
        height: AppSizing.minTapTarget,
        width: width,
        child: child,
      ),
    };

    // The label is announced even while loading, when the visible content is a
    // spinner that reads as nothing.
    return Semantics(
      button: true,
      enabled: _isEnabled,
      label: label,
      child: ExcludeSemantics(child: button),
    );
  }

  Widget? _buildLeading() {
    if (isLoading) {
      return const SizedBox.square(
        dimension: 16,
        child: CircularProgressIndicator(
          strokeWidth: 2,
          color: AppColors.surface,
        ),
      );
    }

    return icon == null ? null : Icon(icon, size: 16);
  }
}
