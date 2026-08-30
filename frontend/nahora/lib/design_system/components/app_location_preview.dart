import 'package:flutter/material.dart';

import '../theme/app_colors.dart';
import '../theme/app_radius.dart';
import '../theme/app_spacing.dart';
import '../theme/app_typography.dart';

/// Where the punch is about to be recorded.
///
/// Shows the map when tiles are available and a plain statement when they are
/// not. The fallback is not a degraded experience to apologise for: latitude
/// and longitude are captured and sent regardless, so a map provider being
/// unreachable — or unconfigured — must never look like the punch is at risk.
class AppLocationPreview extends StatelessWidget {
  const AppLocationPreview({
    required this.accuracyMeters,
    required this.height,
    super.key,
    this.isDegraded = false,
    this.isUnavailable = false,
  });

  final double? accuracyMeters;
  final double height;
  final bool isDegraded;
  final bool isUnavailable;

  @override
  Widget build(BuildContext context) {
    final (tone, icon, message) = switch ((isUnavailable, isDegraded)) {
      (true, _) => (
        AppColors.textSecondary,
        Icons.location_disabled_outlined,
        'Sem sinal de GPS',
      ),
      (_, true) => (
        AppColors.warning,
        Icons.warning_amber_rounded,
        'Precisão baixa · ${accuracyMeters?.round()} m',
      ),
      _ => (
        AppColors.success,
        Icons.check_circle_outline,
        'Localização confirmada · ${accuracyMeters?.round()} m',
      ),
    };

    return Semantics(
      label: message,
      child: ExcludeSemantics(
        child: Container(
          height: height,
          decoration: BoxDecoration(
            color: AppColors.surfaceMuted,
            borderRadius: BorderRadius.circular(AppRadius.md),
            border: Border.all(color: AppColors.border),
          ),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(icon, size: 30, color: tone),
              const SizedBox(height: AppSpacing.sm),
              Text(
                message,
                textAlign: TextAlign.center,
                style: AppTypography.bodyMedium.copyWith(
                  color: tone,
                  fontWeight: FontWeight.w600,
                ),
              ),
              const SizedBox(height: AppSpacing.xs),
              Text(
                'O mapa aparece aqui',
                style: AppTypography.labelSmall.copyWith(
                  color: AppColors.textDisabled,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
