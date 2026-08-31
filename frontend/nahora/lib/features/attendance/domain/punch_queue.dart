import 'pending_punch.dart';

/// Punches held on the device until there is signal.
///
/// A port, because where they are held is a security decision rather than a
/// domain one: a punch carries a coordinate, and a coordinate beside an
/// employee says where that person was at that minute.
abstract interface class PunchQueue {
  /// Everything still waiting, oldest first.
  ///
  /// Order matters: the server decides the direction of each mark from the ones
  /// already recorded, so sending the afternoon's exit before the morning's
  /// entry would label both wrongly.
  Future<List<PendingPunch>> pending();

  Future<void> add(PendingPunch punch);

  /// Drops one that made it through, by the identifier it was sent with.
  Future<void> remove(String clientId);

  Future<void> clear();
}
