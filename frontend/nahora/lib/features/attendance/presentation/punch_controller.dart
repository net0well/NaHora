import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/errors/app_exception.dart';
import '../domain/captured_location.dart';
import '../domain/pending_punch.dart';
import '../domain/location_provider.dart';
import 'attendance_providers.dart';
import 'punch_state.dart';

/// Drives the punch screen.
///
/// Holds no `BuildContext`, so every transition here can be driven in a test
/// without rendering anything — which matters because the interesting states
/// are the ones a GPS will not reproduce on demand.
class PunchController extends AutoDisposeNotifier<PunchState> {
  @override
  PunchState build() {
    Future.microtask(prepare);

    // Opening the screen is the most likely moment for signal to be back: the
    // person walked out of the basement and reached for the app.
    Future.microtask(() => ref.read(punchSyncProvider).drain());

    return const PunchLocating();
  }

  /// Gets permission and a first fix.
  Future<void> prepare() async {
    final provider = ref.read(locationProviderProvider);

    var permission = await provider.checkPermission();
    if (permission == LocationPermissionState.denied) {
      permission = await provider.requestPermission();
    }

    if (permission != LocationPermissionState.granted) {
      state = PunchPermissionDenied(
        isPermanent: permission == LocationPermissionState.deniedForever,
      );

      return;
    }

    if (!await provider.isServiceEnabled()) {
      state = const PunchLocationUnavailable('O GPS está desligado.');

      return;
    }

    await refreshLocation();
  }

  /// Reads the position again.
  ///
  /// A failure here lands on [PunchLocationUnavailable], which still allows the
  /// punch. Anything else would be the client refusing a registration.
  Future<void> refreshLocation() async {
    state = const PunchLocating();

    try {
      final location = await ref
          .read(locationProviderProvider)
          .currentLocation();

      state = location.isDegraded
          ? PunchLowAccuracy(location)
          : PunchReady(location);
    } on Object {
      state = const PunchLocationUnavailable('Sem sinal de GPS no momento.');
    }
  }

  /// Sends the punch, reading the position once more first.
  ///
  /// The identifier is generated here and kept for the whole attempt, so a
  /// retry after a timeout reaches the server as the same punch rather than a
  /// second one.
  Future<void> punch() async {
    if (!state.canPunch) {
      return;
    }

    final shown = _locationOn(state);

    state = const PunchSubmitting();

    // Read again, immediately before sending. The fix drawn on the map can be
    // minutes old by the time a thumb finishes the hold, and what counts is
    // where the person was when they marked — not where they were when they
    // opened the screen.
    final location = await _readAgain() ?? shown;

    if (location == null) {
      state = const PunchFailed(
        'Não conseguimos sua localização. Verifique se o GPS está ligado.',
      );

      return;
    }

    final clientId = ref.read(clientIdFactoryProvider)();
    final deviceId = await ref.read(deviceIdProvider.future);
    final deviceClockAt = DateTime.now();

    try {
      final receipt = await ref
          .read(attendanceRepositoryProvider)
          .registerPunch(
            clientId: clientId,
            location: location,
            deviceId: deviceId,
            deviceClockAt: deviceClockAt,
          );

      state = PunchConfirmed(receipt);

      // Signal is back, so anything the queue is still holding goes now.
      await ref.read(punchSyncProvider).drain();
    } on NetworkException {
      state = PunchQueued(
        await _hold(
          clientId: clientId,
          location: location,
          deviceId: deviceId,
          deviceClockAt: deviceClockAt,
        ),
      );
    } on AppException catch (exception) {
      state = PunchFailed(ErrorMessages.resolve(exception.code));
    }
  }

  /// Keeps the punch on the device until there is signal.
  ///
  /// Returns how many are now waiting. The mark exists from this moment: a
  /// client that answered "no network, try later" would be refusing a
  /// registration, which Art. 78 §1º does not allow it to do.
  Future<int> _hold({
    required String clientId,
    required CapturedLocation location,
    required String deviceId,
    required DateTime deviceClockAt,
  }) async {
    final clock = ref.read(serverClockProvider);
    final queue = ref.read(punchQueueProvider);

    // The anchor is what makes the estimate auditable. Without one — the app
    // never reached the server this session — the device clock is all there is,
    // and sending it with the anchor equal to it is what says so plainly.
    final official = clock.nowUtc() ?? deviceClockAt.toUtc();

    await queue.add(
      PendingPunch(
        clientId: clientId,
        latitude: location.latitude,
        longitude: location.longitude,
        accuracyMeters: location.accuracyMeters,
        capturedAt: location.capturedAt,
        isMocked: location.isMocked,
        deviceId: deviceId,
        deviceClockAt: deviceClockAt.toUtc(),
        occurredAtEstimate: official,
        monotonicElapsedMs: clock.monotonicElapsedMs,
        anchorAt: clock.anchorAt ?? official,
      ),
    );

    return (await queue.pending()).length;
  }

  /// Returns to a state where the employee can punch again.
  Future<void> reset() => refreshLocation();

  /// The fix a state is showing, or null when it is showing none.
  static CapturedLocation? _locationOn(PunchState state) => switch (state) {
    PunchReady(:final location) => location,
    PunchLowAccuracy(:final location) => location,
    _ => null,
  };

  /// One more reading, on the way out.
  ///
  /// A failure here is not a reason to refuse: the punch goes with the fix
  /// already on screen, and if there is none the server still records the mark
  /// and classifies it. Nothing on the client may stop a registration —
  /// Portaria 671, Art. 78 §1º.
  Future<CapturedLocation?> _readAgain() async {
    try {
      return await ref.read(locationProviderProvider).currentLocation();
    } on Object {
      return null;
    }
  }
}
