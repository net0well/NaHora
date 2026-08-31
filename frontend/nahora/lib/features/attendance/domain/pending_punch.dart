/// A punch waiting for signal.
///
/// Everything the server needs, captured at the moment the person marked. The
/// estimate and the anchor travel together so the time can be audited rather
/// than merely believed.
class PendingPunch {
  const PendingPunch({
    required this.clientId,
    required this.latitude,
    required this.longitude,
    required this.accuracyMeters,
    required this.capturedAt,
    required this.isMocked,
    required this.deviceId,
    required this.deviceClockAt,
    required this.occurredAtEstimate,
    required this.monotonicElapsedMs,
    required this.anchorAt,
  });

  factory PendingPunch.fromJson(Map<String, dynamic> json) => PendingPunch(
    clientId: json['clientId'] as String,
    latitude: (json['latitude'] as num).toDouble(),
    longitude: (json['longitude'] as num).toDouble(),
    accuracyMeters: (json['accuracyMeters'] as num).toDouble(),
    capturedAt: DateTime.parse(json['capturedAt'] as String).toUtc(),
    isMocked: (json['isMocked'] as bool?) ?? false,
    deviceId: json['deviceId'] as String,
    deviceClockAt: DateTime.parse(json['deviceClockAt'] as String).toUtc(),
    occurredAtEstimate: DateTime.parse(json['occurredAtEstimate'] as String)
        .toUtc(),
    monotonicElapsedMs: json['monotonicElapsedMs'] as int,
    anchorAt: DateTime.parse(json['anchorAt'] as String).toUtc(),
  );

  Map<String, dynamic> toJson() => {
    'clientId': clientId,
    'latitude': latitude,
    'longitude': longitude,
    'accuracyMeters': accuracyMeters,
    'capturedAt': capturedAt.toUtc().toIso8601String(),
    'isMocked': isMocked,
    'deviceId': deviceId,
    'deviceClockAt': deviceClockAt.toUtc().toIso8601String(),
    'occurredAtEstimate': occurredAtEstimate.toUtc().toIso8601String(),
    'monotonicElapsedMs': monotonicElapsedMs,
    'anchorAt': anchorAt.toUtc().toIso8601String(),
  };

  /// What makes the retry safe. The same punch sent twice arrives as one.
  final String clientId;

  final double latitude;
  final double longitude;
  final double accuracyMeters;
  final DateTime capturedAt;

  /// The platform said the position came from a mock provider. Carried, never
  /// acted on: the server decides what it means.
  final bool isMocked;

  final String deviceId;

  /// What the device believed the time was, as evidence rather than as truth.
  final DateTime deviceClockAt;

  /// The moment the app derived from its monotonic anchor.
  ///
  /// Accepted by the server as an estimate, because the alternative is worse:
  /// stamping arrival time would record a punch made at 07:00 as 14:00, when
  /// signal came back.
  final DateTime occurredAtEstimate;

  final int monotonicElapsedMs;

  /// The server instant the estimate counts from.
  final DateTime anchorAt;
}
