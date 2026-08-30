import 'package:flutter/material.dart';

import '../theme/app_colors.dart';
import '../theme/app_radius.dart';
import '../theme/app_spacing.dart';
import '../theme/app_typography.dart';
import 'status_tone.dart';

/// One recorded punch in a list.
///
/// The time is the point, so it gets tabular figures and the strongest weight
/// on the row. The direction sits at the end because it is what the reader
/// checks second — after confirming the time looks right.
class PunchEntryTile extends StatelessWidget {
  const PunchEntryTile({
    required this.time,
    required this.direction,
    super.key,
    this.tone = StatusTone.success,
  });

  final String time;
  final String direction;
  final StatusTone tone;

  @override
  Widget build(BuildContext context) {
    final (background, foreground, icon) = switch (tone) {
      StatusTone.success => (
        AppColors.primaryLight,
        AppColors.primaryInk,
        Icons.check_circle_outline,
      ),
      StatusTone.warning => (
        AppColors.warningLight,
        AppColors.warning,
        Icons.warning_amber_rounded,
      ),
      StatusTone.danger => (
        AppColors.dangerLight,
        AppColors.danger,
        Icons.cancel_outlined,
      ),
      _ => (
        AppColors.surfaceMuted,
        AppColors.textSecondary,
        Icons.remove_circle_outline,
      ),
    };

    return Semantics(
      label: '$direction às $time',
      child: ExcludeSemantics(
        child: Container(
          padding: const EdgeInsets.symmetric(
            horizontal: AppSpacing.md,
            vertical: AppSpacing.sm + 2,
          ),
          decoration: BoxDecoration(
            color: background,
            borderRadius: BorderRadius.circular(AppRadius.sm),
          ),
          child: Row(
            children: [
              Icon(icon, size: 16, color: foreground),
              const SizedBox(width: AppSpacing.sm),
              Text(
                time,
                style: AppTypography.bodyLarge.copyWith(
                  color: foreground,
                  fontWeight: FontWeight.w500,
                  fontFeatures: const [FontFeature.tabularFigures()],
                ),
              ),
              const SizedBox(width: AppSpacing.sm),

              // Flexible, not a Spacer: the direction can carry a reason after
              // it — "Entrada · Precisão baixa" — and on a phone that runs past
              // the edge. The time keeps its full width because it is the point
              // of the row.
              Expanded(
                child: Text(
                  direction,
                  textAlign: TextAlign.end,
                  overflow: TextOverflow.ellipsis,
                  style: AppTypography.labelSmall.copyWith(
                    color: foreground.withValues(alpha: 0.75),
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
