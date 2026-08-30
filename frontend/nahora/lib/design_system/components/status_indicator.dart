import 'package:flutter/material.dart';

import '../theme/app_colors.dart';
import '../theme/app_spacing.dart';
import '../theme/app_typography.dart';
import 'status_tone.dart';

/// A live condition — a punch validated, a connection lost, a review pending.
///
/// Renders colour **and** icon **and** text, always. This is the rule spec 010
/// states outright, and the reason is concrete: this component reports the
/// state of the camera frame and of a punch, and a user who cannot distinguish
/// the colours would be left with an indicator that conveys nothing.
class StatusIndicator extends StatelessWidget {
  const StatusIndicator({required this.label, required this.tone, super.key});

  final String label;
  final StatusTone tone;

  @override
  Widget build(BuildContext context) {
    final (color, icon) = switch (tone) {
      StatusTone.neutral => (
        AppColors.textSecondary,
        Icons.remove_circle_outline,
      ),
      StatusTone.success => (AppColors.success, Icons.check_circle),
      StatusTone.warning => (AppColors.warning, Icons.warning_amber_rounded),
      StatusTone.danger => (AppColors.danger, Icons.cancel),
      StatusTone.inProgress => (AppColors.info, Icons.sync),
    };

    return Semantics(
      liveRegion: true,
      label: label,
      child: ExcludeSemantics(
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(icon, size: 16, color: color),
            const SizedBox(width: AppSpacing.xs),
            Text(label, style: AppTypography.labelSmall.copyWith(color: color)),
          ],
        ),
      ),
    );
  }
}
