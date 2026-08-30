import 'package:flutter/material.dart';
import 'package:shadcn_ui/shadcn_ui.dart';

import '../theme/app_colors.dart';
import '../theme/app_spacing.dart';
import '../theme/app_typography.dart';
import 'app_badge_tone.dart';

/// A short status label.
///
/// Always renders an icon beside the text. A badge that distinguishes
/// "validado" from "reprovado" by colour alone says nothing to the roughly 8%
/// of men with a colour vision deficiency — and status is exactly what this
/// component exists to communicate.
class AppBadge extends StatelessWidget {
  const AppBadge({
    required this.label,
    super.key,
    this.tone = AppBadgeTone.neutral,
  });

  /// Two or three words. A badge lays its content out in a row that does not
  /// wrap, so a sentence here runs past the edge on a phone — the explanation
  /// belongs in the text around it.
  final String label;

  final AppBadgeTone tone;

  @override
  Widget build(BuildContext context) {
    final (background, foreground, icon) = switch (tone) {
      AppBadgeTone.neutral => (
        AppColors.surfaceMuted,
        AppColors.textSecondary,
        Icons.info_outline,
      ),
      AppBadgeTone.success => (
        AppColors.successLight,
        AppColors.success,
        Icons.check_circle_outline,
      ),
      AppBadgeTone.warning => (
        AppColors.warningLight,
        AppColors.warning,
        Icons.warning_amber_outlined,
      ),
      AppBadgeTone.danger => (
        AppColors.dangerLight,
        AppColors.danger,
        Icons.cancel_outlined,
      ),
    };

    return ShadBadge.raw(
      variant: ShadBadgeVariant.secondary,
      backgroundColor: background,
      foregroundColor: foreground,
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 14, color: foreground),
          const SizedBox(width: AppSpacing.xs),
          Text(
            label,
            style: AppTypography.labelSmall.copyWith(color: foreground),
          ),
        ],
      ),
    );
  }
}
