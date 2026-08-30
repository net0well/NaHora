/// What a button is for, which decides how it looks.
enum AppButtonVariant {
  /// The one action the screen exists for. At most one per screen.
  primary,

  /// A real alternative to the primary action, not a lesser version of it.
  secondary,

  /// Destroys something. Never the default focus.
  danger,

  /// A link in disguise, for navigation that is not the point of the screen.
  text,
}
