/// A position read from the device, on its way to the server.
class CapturedLocation {
  const CapturedLocation({
    required this.latitude,
    required this.longitude,
    required this.accuracyMeters,
    required this.capturedAt,
    required this.isMocked,
  });

  final double latitude;
  final double longitude;

  /// The radius the device reported as its confidence.
  final double accuracyMeters;

  final DateTime capturedAt;

  /// The platform said the position came from a mock provider.
  ///
  /// Reported, never acted on: the app sends the punch either way, and the
  /// server decides what the flag means. A client that refused here would be
  /// the restriction Portaria 671 forbids.
  final bool isMocked;

  /// Beyond this the fix stops narrowing anything useful — a hundred metres
  /// covers a city block. Mirrors the server's own threshold so the screen can
  /// warn before sending rather than after.
  static const double degradedAccuracyMeters = 100;

  bool get isDegraded => accuracyMeters > degradedAccuracyMeters;
}
