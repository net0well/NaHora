import 'punch_direction.dart';
import 'punch_trust.dart';

/// One punch already recorded, as the app reads it back.
///
/// Carries no coordinates. The server does not send them, and the app has no
/// reason to hold a map of where the person has been on the device most likely
/// to be lost or taken.
class PunchEntry {
  const PunchEntry({
    required this.id,
    required this.nsr,
    required this.direction,
    required this.trust,
    required this.occurredAt,
    required this.companyOffset,
    required this.accuracyMeters,
    required this.isOffline,
    this.localDate,
  });

  factory PunchEntry.fromJson(Map<String, dynamic> json) => PunchEntry(
    id: json['id'] as String,
    nsr: json['nsr'] as int,
    direction: PunchDirection.fromCode(json['direction'] as String),
    trust: PunchTrust.fromCode(json['trust'] as String),
    occurredAt: DateTime.parse(json['occurredAt'] as String).toUtc(),
    companyOffset: Duration(seconds: (json['utcOffsetSeconds'] as int?) ?? 0),
    accuracyMeters: (json['accuracyMeters'] as num?)?.toDouble() ?? 0,
    isOffline: json['origin'] == 'Offline',
    localDate: json['localDate'] as String?,
  );

  final String id;

  /// The sequential number in the company's record. What an inspection cites.
  final int nsr;

  final PunchDirection direction;

  final PunchTrust trust;

  /// The moment that counts, in UTC. Rendered in company time by the screen.
  final DateTime occurredAt;

  /// The company offset in force when this punch happened.
  ///
  /// Sent by the server with the punch itself rather than read from a clock
  /// synchronised elsewhere: the screen can render before that clock has
  /// answered, and a punch made before a daylight-saving change keeps the hour
  /// it was actually recorded at.
  final Duration companyOffset;

  final double accuracyMeters;

  /// Whether it waited in the device queue before reaching the server.
  final bool isOffline;

  /// The working day it belongs to, decided on the server in company time.
  ///
  /// Only the history carries it: the day list already knows which day it is,
  /// and recomputing it from a timestamp on a device in another timezone would
  /// split one working day into two.
  final String? localDate;
}
