import 'package:nahora/features/attendance/domain/pending_punch.dart';
import 'package:nahora/features/attendance/domain/punch_queue.dart';

/// A queue a test can look inside.
class FakePunchQueue implements PunchQueue {
  final List<PendingPunch> held = [];

  @override
  Future<void> add(PendingPunch punch) async => held.add(punch);

  @override
  Future<List<PendingPunch>> pending() async => List.of(held);

  @override
  Future<void> remove(String clientId) async =>
      held.removeWhere((punch) => punch.clientId == clientId);

  @override
  Future<void> clear() async => held.clear();
}
