import 'package:flutter/material.dart';

import '../theme/app_colors.dart';
import '../theme/app_spacing.dart';
import '../theme/app_typography.dart';
import 'app_button.dart';
import 'app_button_variant.dart';

/// Something failed, and here is what to do about it.
///
/// Takes a message already translated for a person. Technical detail and stack
/// traces go to the log; a user who reads "SocketException" learns nothing and
/// calls support.
///
/// Retry is offered whenever the caller can retry, because the honest answer to
/// most transient failures is "try again" and a screen that only apologises
/// forces the user to guess that.
class AppErrorState extends StatelessWidget {
  const AppErrorState({
    required this.message,
    super.key,
    this.title = 'Algo deu errado',
    this.onRetry,
    this.retryLabel = 'Tentar novamente',
  });

  final String message;
  final String title;
  final VoidCallback? onRetry;
  final String retryLabel;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        // Horizontal padding is smaller than vertical on purpose: these states
        // sit inside screens that already pad, and doubling it left a phone
        // with less width than the retry button needs.
        padding: const EdgeInsets.symmetric(
          horizontal: AppSpacing.md,
          vertical: AppSpacing.xl,
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.error_outline, size: 48, color: AppColors.danger),
            const SizedBox(height: AppSpacing.md),
            Text(
              title,
              style: AppTypography.titleMedium,
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: AppSpacing.sm),
            Text(
              message,
              textAlign: TextAlign.center,
              style: AppTypography.bodyMedium.copyWith(
                color: AppColors.textSecondary,
              ),
            ),
            if (onRetry != null) ...[
              const SizedBox(height: AppSpacing.lg),
              // No icon. This state is used inside cards, where the label
              // plus an icon is wider than the space a phone leaves, and the
              // button then runs past the edge of the card.
              AppButton(
                label: retryLabel,
                onPressed: onRetry,
                variant: AppButtonVariant.secondary,
              ),
            ],
          ],
        ),
      ),
    );
  }
}
