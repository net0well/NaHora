import 'dart:math';

import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network_providers.dart';
import '../data/attendance_repository.dart';
import '../data/geolocator_location_provider.dart';
import '../data/server_clock.dart';
import '../domain/location_provider.dart';
import 'punch_controller.dart';
import 'punch_state.dart';

final locationProviderProvider = Provider<LocationProvider>(
  (ref) => GeolocatorLocationProvider(),
);

final attendanceRepositoryProvider = Provider<AttendanceRepository>(
  (ref) => AttendanceRepository(client: ref.watch(apiClientProvider)),
);

/// Kept for the app's lifetime: the monotonic anchor is only useful if it keeps
/// counting, and a clock rebuilt per screen would lose it.
final serverClockProvider = Provider<ServerClock>((ref) => ServerClock());

/// Generates the identifier that makes a retry idempotent.
///
/// A provider rather than a direct call so a test can make it predictable and
/// assert that one attempt produces one identifier.
final clientIdFactoryProvider = Provider<String Function()>((ref) {
  final random = Random.secure();

  return () => List.generate(
    16,
    (_) => random.nextInt(256).toRadixString(16).padLeft(2, '0'),
  ).join();
});

/// Identifies the installation, not the person.
///
/// Enough to notice that four employees punch from one handset; not a
/// credential, and not stable across a reinstall.
final deviceIdProvider = FutureProvider<String>(
  (ref) async => 'android-device',
);

final punchControllerProvider =
    AutoDisposeNotifierProvider<PunchController, PunchState>(
      PunchController.new,
    );
