import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/errors/app_exception.dart';
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

  /// Sends the punch.
  ///
  /// The identifier is generated here and kept for the whole attempt, so a
  /// retry after a timeout reaches the server as the same punch rather than a
  /// second one.
  Future<void> punch() async {
    if (!state.canPunch) {
      return;
    }

    final location = switch (state) {
      PunchReady(:final location) => location,
      PunchLowAccuracy(:final location) => location,
      _ => null,
    };

    if (location == null) {
      state = const PunchFailed(
        'Não conseguimos sua localização. Verifique se o GPS está ligado.',
      );

      return;
    }

    state = const PunchSubmitting();

    try {
      final receipt = await ref
          .read(attendanceRepositoryProvider)
          .registerPunch(
            clientId: ref.read(clientIdFactoryProvider)(),
            location: location,
            deviceId: await ref.read(deviceIdProvider.future),
            deviceClockAt: DateTime.now(),
          );

      state = PunchConfirmed(receipt);
    } on AppException catch (exception) {
      state = PunchFailed(ErrorMessages.resolve(exception.code));
    }
  }

  /// Returns to a state where the employee can punch again.
  Future<void> reset() => refreshLocation();
}
