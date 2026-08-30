import 'punch_direction.dart';
import 'punch_entry.dart';

/// One working day and what was recorded in it.
class PunchDay {
  const PunchDay({
    required this.localDate,
    required this.timeZoneId,
    required this.nextDirection,
    required this.entries,
  });

  factory PunchDay.fromJson(Map<String, dynamic> json) => PunchDay(
    localDate: json['localDate'] as String,
    timeZoneId: json['timeZoneId'] as String,
    nextDirection: PunchDirection.fromCode(json['nextDirection'] as String),
    entries: [
      for (final entry in (json['punches'] as List<dynamic>? ?? const []))
        PunchEntry.fromJson(entry as Map<String, dynamic>),
    ],
  );

  /// The day these belong to, in company time, as decided by the server.
  final String localDate;

  final String timeZoneId;

  /// Which boundary the next punch would mark.
  ///
  /// Advisory. The server decides again when the punch actually arrives, so a
  /// screen left open overnight cannot talk it into recording the wrong one.
  final PunchDirection nextDirection;

  final List<PunchEntry> entries;

  bool get isEmpty => entries.isEmpty;
}
