import 'package:flutter/material.dart';

/// Colour tokens. No widget outside this file declares a literal colour — a
/// lint rule enforces it, so a restyle never turns into a search through
/// hundreds of widgets.
abstract final class AppColors {
  // --- Brand ---------------------------------------------------------------
  static const Color primary = Color(0xFF1B5FA8);
  static const Color primaryDark = Color(0xFF124477);
  static const Color primaryLight = Color(0xFFE3EEF9);

  // --- Neutral -------------------------------------------------------------
  static const Color surface = Color(0xFFFFFFFF);
  static const Color surfaceMuted = Color(0xFFF4F6F9);
  static const Color border = Color(0xFFD9DEE6);
  static const Color textPrimary = Color(0xFF16202C);
  static const Color textSecondary = Color(0xFF5B6675);
  static const Color textDisabled = Color(0xFF9AA3AF);

  // --- Semantic ------------------------------------------------------------
  static const Color success = Color(0xFF1B7F4B);
  static const Color successLight = Color(0xFFE6F4EC);
  static const Color warning = Color(0xFFB26A00);
  static const Color warningLight = Color(0xFFFDF1E0);
  static const Color danger = Color(0xFFB3261E);
  static const Color dangerLight = Color(0xFFFBEAE9);
  static const Color info = Color(0xFF1B5FA8);

  /// Camera frame states.
  ///
  /// Colour alone never carries the meaning: every state also renders an icon
  /// and a text label, because roughly 8% of men have some colour vision
  /// deficiency and a red frame would tell them nothing.
  static const Color frameIdle = Color(0xFF9AA3AF);
  static const Color frameAdjusting = Color(0xFFB26A00);
  static const Color frameChallenge = Color(0xFF1B5FA8);
  static const Color frameSuccess = Color(0xFF1B7F4B);
  static const Color frameFailure = Color(0xFFB3261E);
  static const Color frameSuspicious = Color(0xFF8A6100);
}
