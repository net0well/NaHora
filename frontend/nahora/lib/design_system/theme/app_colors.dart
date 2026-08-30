import 'package:flutter/material.dart';

/// Colour tokens. No widget outside this file declares a literal colour — a
/// lint rule enforces it, so a restyle never turns into a search through
/// hundreds of widgets.
///
/// Values taken from the approved Figma reference. The product runs on a dark
/// ground with an amber accent, which suits where it is used: a phone held at
/// the entrance of a warehouse or a site, often before sunrise or after dark.
abstract final class AppColors {
  // --- Brand ---------------------------------------------------------------
  //
  // Amber carries **dark** ink, never white. White on this fails contrast
  // badly, and the punch button — the most important control in the product —
  // is exactly where that would show.
  static const Color primary = Color(0xFFFDB623);
  static const Color primaryDark = Color(0xFFD9930C);

  /// The accent at low opacity, for washes behind amber text.
  static const Color primaryLight = Color(0xFF3A3223);

  /// Text and icons drawn on top of [primaryLight].
  static const Color primaryInk = Color(0xFFFDB623);

  /// Text and icons drawn on top of [primary]. Dark by necessity.
  static const Color onPrimary = Color(0xFF231A08);

  // --- Neutral -------------------------------------------------------------
  //
  // Two greys rather than one: the ground sits slightly darker than the cards
  // and the tab bar, so a raised surface reads as raised without a border.
  static const Color background = Color(0xFF2A2A2A);
  static const Color surface = Color(0xFF333333);
  static const Color surfaceMuted = Color(0xFF3B3B3B);
  static const Color border = Color(0xFF4A4A4A);
  static const Color textPrimary = Color(0xFFD9D9D9);
  static const Color textSecondary = Color(0xFF9A9A9A);
  static const Color textDisabled = Color(0xFF6E6E6E);

  /// Ink for the rare light chip, taken from the reference's own variable.
  static const Color onLightChip = Color(0xFF2D3047);

  // --- Semantic ------------------------------------------------------------
  //
  // Lifted for a dark ground. The light-theme greens and reds were tuned to
  // carry on white and go muddy here, so each one is brightened rather than
  // reused — semantic colour has to stay legible, which is the whole point of
  // it being separate from the brand.
  static const Color success = Color(0xFF4ADE80);
  static const Color successLight = Color(0xFF1E3A2A);
  static const Color warning = Color(0xFFFBBF24);
  static const Color warningLight = Color(0xFF3A3117);
  static const Color danger = Color(0xFFF87171);
  static const Color dangerLight = Color(0xFF3B2020);

  /// Informational, and deliberately not the brand.
  ///
  /// Success, warning, danger and information have to mean the same thing
  /// regardless of what colour the company is painted in.
  static const Color info = Color(0xFF60A5FA);

  /// Punch states.
  ///
  /// Colour alone never carries the meaning: every state also renders an icon
  /// and a text label, because roughly 8% of men have some colour vision
  /// deficiency and a red indicator would tell them nothing.
  static const Color punchLocating = textSecondary;
  static const Color punchLowAccuracy = warning;
  static const Color punchReady = primary;
  static const Color punchConfirmed = success;
  static const Color punchFailed = danger;

  /// Recorded on the device, not yet sent. Presented as success, never as an
  /// error: the punch exists, it simply has not synced.
  static const Color punchQueued = Color(0xFFD8A24A);
}
