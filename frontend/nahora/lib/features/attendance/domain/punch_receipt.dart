import 'punch_direction.dart';

/// The server's confirmation of a recorded punch.
class PunchReceipt {
  const PunchReceipt({
    required this.id,
    required this.nsr,
    required this.direction,
    required this.occurredAt,
    required this.trust,
    required this.alreadyRegistered,
  });

  final String id;

  /// Sequential number in the company. What the AFD is built from.
  final int nsr;

  final PunchDirection direction;

  /// The time that counts — the server's, never the device's.
  final DateTime occurredAt;

  final String trust;

  /// The server had already recorded this punch; this was a retry.
  final bool alreadyRegistered;
}
