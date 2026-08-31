/// Where one mark was made.
///
/// Fetched on request, one mark at a time. No listing carries coordinates: a
/// list that did would hand the device a map of everywhere the person has been,
/// and the device is the part of the system most likely to be lost or taken.
class PunchLocation {
  const PunchLocation({
    required this.punchId,
    required this.latitude,
    required this.longitude,
    required this.accuracyMeters,
    required this.capturedAt,
  });

  factory PunchLocation.fromJson(Map<String, dynamic> json) => PunchLocation(
    punchId: json['punchId'] as String,
    latitude: (json['latitude'] as num).toDouble(),
    longitude: (json['longitude'] as num).toDouble(),
    accuracyMeters: (json['accuracyMeters'] as num).toDouble(),
    capturedAt: DateTime.parse(json['capturedAt'] as String).toUtc(),
  );

  final String punchId;
  final double latitude;
  final double longitude;

  /// The radius the reading was good to, drawn as a circle around the pin.
  final double accuracyMeters;

  /// When the device read the position, which is not when the punch was
  /// recorded. A gap between the two is what a stale fix looks like.
  final DateTime capturedAt;
}
