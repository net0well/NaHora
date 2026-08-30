/// What a badge is saying, which decides its colour.
enum AppBadgeTone {
  /// Neutral state — informative, no judgement.
  neutral,

  /// Confirmed, valid, complete.
  success,

  /// Needs attention but nothing failed.
  warning,

  /// Failed, rejected, or revoked.
  danger,
}
