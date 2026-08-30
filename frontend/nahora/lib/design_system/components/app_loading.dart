import 'package:flutter/material.dart';

import '../theme/app_colors.dart';
import '../theme/app_spacing.dart';
import '../theme/app_typography.dart';

/// Work in progress.
///
/// The message is required rather than optional. A bare spinner tells the user
/// that something is happening but not what, and "not what" is the difference
/// between waiting and giving up.
class AppLoading extends StatelessWidget {
  const AppLoading({required this.message, super.key});

  final String message;

  @override
  Widget build(BuildContext context) {
    // The message is announced once, by the Semantics node. Without excluding
    // the visual tree the Text below would announce it a second time.
    return Semantics(
      liveRegion: true,
      label: message,
      child: ExcludeSemantics(
        child: Center(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const SizedBox.square(
                dimension: 28,
                child: CircularProgressIndicator(
                  strokeWidth: 3,
                  color: AppColors.primary,
                ),
              ),
              const SizedBox(height: AppSpacing.md),
              Text(
                message,
                textAlign: TextAlign.center,
                style: AppTypography.bodyMedium.copyWith(
                  color: AppColors.textSecondary,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
