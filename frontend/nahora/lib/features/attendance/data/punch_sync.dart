import '../../../core/errors/app_exception.dart';
import '../domain/captured_location.dart';
import '../domain/punch_queue.dart';
import 'attendance_repository.dart';

// Named parameters keep the call site readable while the fields stay private.
// An initializing formal would expose `_queue` as the parameter name.
// ignore_for_file: prefer_initializing_formals

/// Sends what the queue is holding, in the order it was made.
///
/// Order is not cosmetic: the server decides the direction of each mark from
/// the ones already recorded, so an afternoon exit sent before a morning entry
/// would label both wrongly.
class PunchSync {
  PunchSync({
    required PunchQueue queue,
    required AttendanceRepository repository,
  }) : _queue = queue,
       _repository = repository;

  final PunchQueue _queue;
  final AttendanceRepository _repository;

  /// Sends everything it can and returns how many got through.
  ///
  /// Stops at the first sign there is still no signal rather than burning
  /// through the queue against a dead network — and stops on an expired
  /// session too, which is a reason to sign in again, not to lose a mark.
  Future<int> drain() async {
    var sent = 0;

    for (final punch in await _queue.pending()) {
      try {
        await _repository.registerPunch(
          clientId: punch.clientId,
          location: CapturedLocation(
            latitude: punch.latitude,
            longitude: punch.longitude,
            accuracyMeters: punch.accuracyMeters,
            capturedAt: punch.capturedAt,
            isMocked: punch.isMocked,
          ),
          deviceId: punch.deviceId,
          deviceClockAt: punch.deviceClockAt,
          offline: OfflineEstimate(
            occurredAt: punch.occurredAtEstimate,
            monotonicElapsedMs: punch.monotonicElapsedMs,
            anchorAt: punch.anchorAt,
          ),
        );

        await _queue.remove(punch.clientId);
        sent++;
      } on NetworkException {
        break;
      } on UnauthorizedException {
        break;
      } on AppException {
        // Refused on its merits rather than by the network — a duplicate, or a
        // body the server will never accept. Kept, it would fail on every drain
        // from now on and hold every later mark behind it.
        await _queue.remove(punch.clientId);
      }
    }

    return sent;
  }
}
