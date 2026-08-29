/// Spacing scale in logical pixels. Widgets use these instead of literals so
/// vertical rhythm stays consistent across screens.
abstract final class AppSpacing {
  static const double xs = 4;
  static const double sm = 8;
  static const double md = 16;
  static const double lg = 24;
  static const double xl = 32;
  static const double xxl = 48;
}

/// Corner radii.
abstract final class AppRadius {
  static const double sm = 8;
  static const double md = 12;
  static const double lg = 16;
  static const double full = 999;
}

/// Animation durations.
abstract final class AppDuration {
  static const Duration fast = Duration(milliseconds: 150);
  static const Duration normal = Duration(milliseconds: 250);
  static const Duration slow = Duration(milliseconds: 400);
}

/// Minimum hit target. Desktop pointers are precise, but the same screens run
/// on touch devices later and 44 logical pixels is the accessible floor.
abstract final class AppSizing {
  static const double minTapTarget = 44;
  static const double buttonHeight = 48;
  static const double inputHeight = 48;
}
