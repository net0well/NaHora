import 'package:flutter/material.dart';
import 'package:shadcn_ui/shadcn_ui.dart';

import 'app_colors.dart';
import 'app_radius.dart';
import 'app_spacing.dart';
import 'app_typography.dart';

/// Builds the themes both widget libraries read.
///
/// The tokens are the source of truth and the libraries are fed from them, not
/// the other way round. That is what keeps the choice of component library
/// reversible: replacing it changes what [shad] returns, not what any screen
/// looks like.
abstract final class AppTheme {
  static ThemeData light() {
    final colorScheme =
        ColorScheme.fromSeed(
          seedColor: AppColors.primary,
          brightness: Brightness.light,
        ).copyWith(
          primary: AppColors.primary,
          surface: AppColors.surface,
          error: AppColors.danger,
        );

    return ThemeData(
      useMaterial3: true,
      colorScheme: colorScheme,
      scaffoldBackgroundColor: AppColors.surfaceMuted,
      fontFamily: AppTypography.fontFamily,
      textTheme: const TextTheme(
        displayLarge: AppTypography.displayLarge,
        headlineMedium: AppTypography.headlineMedium,
        titleLarge: AppTypography.titleLarge,
        titleMedium: AppTypography.titleMedium,
        bodyLarge: AppTypography.bodyLarge,
        bodyMedium: AppTypography.bodyMedium,
        labelLarge: AppTypography.labelLarge,
        labelSmall: AppTypography.labelSmall,
      ),
      appBarTheme: const AppBarTheme(
        backgroundColor: AppColors.surface,
        foregroundColor: AppColors.textPrimary,
        elevation: 0,
        scrolledUnderElevation: 1,
        centerTitle: false,
        titleTextStyle: AppTypography.titleLarge,
      ),
      cardTheme: CardThemeData(
        color: AppColors.surface,
        elevation: 0,
        margin: EdgeInsets.zero,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(AppRadius.md),
          side: const BorderSide(color: AppColors.border),
        ),
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: AppColors.surface,
        contentPadding: const EdgeInsets.symmetric(
          horizontal: AppSpacing.md,
          vertical: AppSpacing.md,
        ),
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(AppRadius.sm),
          borderSide: const BorderSide(color: AppColors.border),
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(AppRadius.sm),
          borderSide: const BorderSide(color: AppColors.border),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(AppRadius.sm),
          borderSide: const BorderSide(color: AppColors.primary, width: 2),
        ),
        errorBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(AppRadius.sm),
          borderSide: const BorderSide(color: AppColors.danger),
        ),
      ),
      dividerTheme: const DividerThemeData(
        color: AppColors.border,
        thickness: 1,
        space: 1,
      ),
    );
  }

  /// The component library's theme, built from the same tokens.
  ///
  /// Every colour here is a token. A value invented at this layer would be a
  /// second source of truth, and two sources of truth for colour produce the
  /// worst possible outcome: an interface that is almost consistent.
  static ShadThemeData shad() {
    return ShadThemeData(
      brightness: Brightness.light,
      colorScheme: const ShadColorScheme(
        background: AppColors.surfaceMuted,
        foreground: AppColors.textPrimary,
        card: AppColors.surface,
        cardForeground: AppColors.textPrimary,
        popover: AppColors.surface,
        popoverForeground: AppColors.textPrimary,
        primary: AppColors.primary,
        primaryForeground: AppColors.surface,
        secondary: AppColors.surfaceMuted,
        secondaryForeground: AppColors.textPrimary,
        muted: AppColors.surfaceMuted,
        mutedForeground: AppColors.textSecondary,
        accent: AppColors.primaryLight,
        accentForeground: AppColors.primaryDark,
        destructive: AppColors.danger,
        destructiveForeground: AppColors.surface,
        border: AppColors.border,
        input: AppColors.border,
        ring: AppColors.primary,
        selection: AppColors.primaryLight,
      ),
      radius: BorderRadius.all(Radius.circular(AppRadius.sm)),
    );
  }
}
