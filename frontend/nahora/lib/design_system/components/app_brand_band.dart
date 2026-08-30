import 'package:flutter/material.dart';

import '../theme/app_colors.dart';
import '../theme/app_spacing.dart';
import '../theme/app_typography.dart';

/// The brand-coloured header that opens most screens.
///
/// Carries who the person is and which company they are punching for. The
/// company line matters more than it looks: somebody who works for two
/// employers through the same app needs to confirm at a glance that they are
/// about to punch for the right one.
class AppBrandBand extends StatelessWidget {
  const AppBrandBand({
    required this.title,
    super.key,
    this.eyebrow,
    this.onBack,
    this.footerLeading,
    this.footerTrailing,
    this.padding,
  });

  /// The line people read first — a name, or what the screen is for.
  final String title;

  /// Small line above the title. A greeting, or the screen's name.
  final String? eyebrow;

  /// Shows a back arrow when given. A screen reached by push needs a visible
  /// way out — the system gesture alone is not an affordance.
  final VoidCallback? onBack;

  final String? footerLeading;
  final String? footerTrailing;

  final EdgeInsets? padding;

  bool get _hasFooter => footerLeading != null || footerTrailing != null;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      color: AppColors.primary,
      padding:
          padding ??
          const EdgeInsets.fromLTRB(
            AppSpacing.lg,
            AppSpacing.md,
            AppSpacing.lg,
            AppSpacing.lg,
          ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        mainAxisSize: MainAxisSize.min,
        children: [
          if (onBack != null)
            Align(
              alignment: Alignment.centerLeft,
              child: IconButton(
                onPressed: onBack,
                icon: const Icon(Icons.arrow_back),
                color: AppColors.onPrimary,
                tooltip: 'Voltar',
                padding: EdgeInsets.zero,
                visualDensity: VisualDensity.compact,
              ),
            ),
          if (eyebrow != null)
            Text(
              eyebrow!,
              style: AppTypography.bodyMedium.copyWith(
                color: AppColors.onPrimary.withValues(alpha: 0.82),
                fontSize: 13,
              ),
            ),
          Semantics(
            header: true,
            child: Text(
              title,
              style: AppTypography.headlineMedium.copyWith(
                color: AppColors.onPrimary,
              ),
            ),
          ),
          if (_hasFooter) ...[
            const SizedBox(height: AppSpacing.sm),
            Divider(
              height: 1,
              thickness: 1,
              color: AppColors.onPrimary.withValues(alpha: 0.22),
            ),
            const SizedBox(height: AppSpacing.sm),
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                if (footerLeading != null)
                  Flexible(
                    child: Text(
                      footerLeading!,
                      overflow: TextOverflow.ellipsis,
                      style: AppTypography.labelSmall.copyWith(
                        color: AppColors.onPrimary.withValues(alpha: 0.85),
                      ),
                    ),
                  ),
                if (footerTrailing != null)
                  Text(
                    footerTrailing!,
                    style: AppTypography.labelSmall.copyWith(
                      color: AppColors.onPrimary.withValues(alpha: 0.85),
                      fontFeatures: const [FontFeature.tabularFigures()],
                    ),
                  ),
              ],
            ),
          ],
        ],
      ),
    );
  }
}
