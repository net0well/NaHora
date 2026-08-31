import 'punch_direction.dart';
import 'punch_entry_source.dart';
import 'punch_trust.dart';

/// One mark of the working day as it counts, after approved adjustments.
///
/// Carries no coordinates. The server does not send them with a listing, and
/// the app has no reason to hold a map of where the person has been on the
/// device most likely to be lost or taken.
class PunchEntry {
  const PunchEntry({
    required this.id,
    required this.direction,
    required this.source,
    required this.occurredAt,
    required this.companyOffset,
    required this.isOffline,
    this.nsr,
    this.trust,
    this.originalOccurredAt,
    this.accuracyMeters,
    this.localDate,
  });

  factory PunchEntry.fromJson(Map<String, dynamic> json) => PunchEntry(
    id: json['id'] as String,
    nsr: json['nsr'] as int?,
    direction: PunchDirection.fromCode(json['direction'] as String),
    source: PunchEntrySource.fromCode(json['source'] as String?),
    trust: json['trust'] == null
        ? null
        : PunchTrust.fromCode(json['trust'] as String),
    occurredAt: DateTime.parse(json['occurredAt'] as String).toUtc(),
    originalOccurredAt: json['originalOccurredAt'] == null
        ? null
        : DateTime.parse(json['originalOccurredAt'] as String).toUtc(),
    companyOffset: Duration(seconds: (json['utcOffsetSeconds'] as int?) ?? 0),
    accuracyMeters: (json['accuracyMeters'] as num?)?.toDouble(),
    isOffline: json['origin'] == 'Offline',
    localDate: json['localDate'] as String?,
  );

  /// The punch, or the adjustment when there is no punch behind it.
  final String id;

  /// The sequential number in the company's record — what an inspection cites.
  ///
  /// Null for an inclusion: nothing was captured, so no registrar numbered it.
  final int? nsr;

  final PunchDirection direction;

  /// Whether this was captured, corrected or granted.
  final PunchEntrySource source;

  /// How much the capture can be relied on. Null for an inclusion, where there
  /// was no capture to trust or doubt.
  final PunchTrust? trust;

  /// The moment that counts, in UTC. Rendered in company time by the screen.
  final DateTime occurredAt;

  /// The time a correction replaced, when one did.
  ///
  /// Kept beside the value in force rather than dropped: a correction that
  /// hides what it replaced is an edit, and the employee has the right to see
  /// that their record was changed and from what.
  final DateTime? originalOccurredAt;

  /// The company offset in force when this mark happened.
  ///
  /// Sent by the server with the mark itself rather than read from a clock
  /// synchronised elsewhere: the screen can render before that clock has
  /// answered, and a punch made before a daylight-saving change keeps the hour
  /// it was actually recorded at.
  final Duration companyOffset;

  /// Null for an inclusion: no position was ever measured.
  final double? accuracyMeters;

  /// Whether it waited in the device queue before reaching the server.
  final bool isOffline;

  /// The working day it belongs to, decided on the server in company time.
  ///
  /// Only the history carries it: the day list already knows which day it is,
  /// and recomputing it from a timestamp on a device in another timezone would
  /// split one working day into two.
  final String? localDate;

  bool get isAdjusted => source == PunchEntrySource.adjusted;

  bool get isIncluded => source == PunchEntrySource.included;
}
