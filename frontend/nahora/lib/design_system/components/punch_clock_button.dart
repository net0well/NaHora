import 'package:flutter/material.dart';

import '../theme/app_colors.dart';
import '../theme/app_radius.dart';
import '../theme/app_spacing.dart';
import '../theme/app_typography.dart';

/// The clock and the action, in one control.
///
/// The best idea in the reference the design came from: the employee looks at
/// the time and their thumb is already where it needs to press. Splitting them
/// puts a glance between the person and the only thing they came to do.
///
/// The time shown is the server's. A phone running fast would otherwise display
/// one time and record another, and the employee would be right to call that a
/// lie.
class PunchClockButton extends StatelessWidget {
  const PunchClockButton({
    required this.dateLabel,
    required this.timeLabel,
    required this.onPressed,
    super.key,
    this.isLoading = false,
    this.compact = false,
  });

  /// Already formatted for a person — "Sex, 30 Ago 2026".
  final String dateLabel;

  /// The official time. Shows placeholder dashes until the clock has synced,
  /// because falling back to the device is the failure this design prevents.
  final String timeLabel;

  final VoidCallback? onPressed;
  final bool isLoading;
  final bool compact;

  bool get _isEnabled => onPressed != null && !isLoading;

  @override
  Widget build(BuildContext context) {
    return Semantics(
      button: true,
      enabled: _isEnabled,
      label: 'Marcar ponto. $dateLabel, $timeLabel',
      child: ExcludeSemantics(
        child: Material(
          color: _isEnabled
              ? AppColors.primary
              : AppColors.primary.withValues(alpha: 0.55),
          borderRadius: BorderRadius.circular(AppRadius.lg),
          child: InkWell(
            onTap: _isEnabled ? onPressed : null,
            borderRadius: BorderRadius.circular(AppRadius.lg),
            child: Padding(
              padding: EdgeInsets.symmetric(
                horizontal: AppSpacing.lg,
                vertical: compact ? AppSpacing.sm + 4 : AppSpacing.md,
              ),
              child: Row(
                children: [
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Text(
                          dateLabel,
                          style: AppTypography.labelSmall.copyWith(
                            color: AppColors.onPrimary.withValues(alpha: 0.85),
                          ),
                        ),
                        const SizedBox(height: AppSpacing.xs / 2),
                        Text(
                          timeLabel,
                          style: AppTypography.displayLarge.copyWith(
                            color: AppColors.onPrimary,
                            fontSize: compact ? 28 : 34,
                            fontWeight: FontWeight.w300,
                            height: 1,
                            fontFeatures: const [FontFeature.tabularFigures()],
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(width: AppSpacing.md),
                  SizedBox.square(
                    dimension: compact ? 30 : 36,
                    child: isLoading
                        ? const CircularProgressIndicator(
                            strokeWidth: 2.4,
                            color: AppColors.onPrimary,
                          )
                        : const Icon(
                            // A clock, not a fingerprint. The product confirms
                            // by location and reads no biometrics, and an icon
                            // that suggests otherwise promises something it
                            // does not deliver.
                            Icons.schedule_outlined,
                            color: AppColors.onPrimary,
                            size: 32,
                          ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
