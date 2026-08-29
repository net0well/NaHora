import 'package:flutter/material.dart';

import '../theme/app_colors.dart';
import '../theme/app_spacing.dart';
import '../theme/app_typography.dart';

enum AppButtonVariant { primary, secondary, danger, text }

/// Application button. Handles its own loading state so callers never have to
/// remember to disable the button while a request is in flight — the double
/// submit that creates a duplicate punch is not left to discipline.
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
    final child = _buildContent();

    final button = switch (variant) {
      AppButtonVariant.primary => FilledButton(
        onPressed: _isEnabled ? onPressed : null,
        style: _style(AppColors.primary, AppColors.surface),
        child: child,
      ),
      AppButtonVariant.secondary => OutlinedButton(
        onPressed: _isEnabled ? onPressed : null,
        style: OutlinedButton.styleFrom(
          foregroundColor: AppColors.primary,
          minimumSize: const Size(0, AppSizing.buttonHeight),
          side: const BorderSide(color: AppColors.primary),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(AppRadius.sm),
          ),
          textStyle: AppTypography.labelLarge,
        ),
        child: child,
      ),
      AppButtonVariant.danger => FilledButton(
        onPressed: _isEnabled ? onPressed : null,
        style: _style(AppColors.danger, AppColors.surface),
        child: child,
      ),
      AppButtonVariant.text => TextButton(
        onPressed: _isEnabled ? onPressed : null,
        style: TextButton.styleFrom(
          foregroundColor: AppColors.primary,
          minimumSize: const Size(0, AppSizing.minTapTarget),
          textStyle: AppTypography.labelLarge,
        ),
        child: child,
      ),
    };

    return Semantics(
      button: true,
      enabled: _isEnabled,
      label: label,
      child: expanded
          ? SizedBox(width: double.infinity, child: button)
          : button,
    );
  }

  ButtonStyle _style(Color background, Color foreground) =>
      FilledButton.styleFrom(
        backgroundColor: background,
        foregroundColor: foreground,
        minimumSize: const Size(0, AppSizing.buttonHeight),
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(AppRadius.sm),
        ),
        textStyle: AppTypography.labelLarge,
      );

  Widget _buildContent() {
    if (isLoading) {
      return const SizedBox(
        height: 18,
        width: 18,
        child: CircularProgressIndicator(strokeWidth: 2),
      );
    }

    if (icon == null) {
      return Text(label);
    }

    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(icon, size: 18),
        const SizedBox(width: AppSpacing.sm),
        Text(label),
      ],
    );
  }
}
