import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/errors/app_exception.dart';
import 'attendance_providers.dart';

/// Keeps the official clock anchored.
///
/// Synchronises once on start and then leaves the monotonic anchor to do the
/// work. Polling the server every second would be both wasteful and pointless:
/// the anchor already advances correctly, and a network round trip is a worse
/// source of the current time than a stopwatch.
class ServerClockController extends AsyncNotifier<void> {
  Duration _offset = Duration.zero;

  /// The company's UTC offset, for rendering a local time.
  ///
  /// Sent by the server rather than derived on the device: a phone with an
  /// outdated timezone database would compute a different one after a
  /// daylight-saving change, and the screen would disagree with the record.
  Duration get offset => _offset;

  @override
  Future<void> build() => synchronise();

  Future<void> synchronise() async {
    try {
      final serverTime = await ref
          .read(attendanceRepositoryProvider)
          .serverTime();

      ref.read(serverClockProvider).synchronise(serverTime.utc);
      _offset = serverTime.utcOffset;
    } on AppException {
      // Left unsynchronised on purpose. The screen shows dashes rather than
      // falling back to the device clock, because falling back is exactly the
      // failure this whole mechanism exists to prevent.
      rethrow;
    }
  }
}

final serverClockControllerProvider =
    AsyncNotifierProvider<ServerClockController, void>(
      ServerClockController.new,
    );
