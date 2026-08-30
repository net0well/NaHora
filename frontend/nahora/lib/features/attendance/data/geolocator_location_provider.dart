import 'package:geolocator/geolocator.dart';

import '../domain/captured_location.dart';
import '../domain/location_provider.dart';

/// Reads the position through the `geolocator` package.
///
/// Named for the technology it adapts rather than for the pattern: swapping the
/// package changes this file, and the name says which file that is.
class GeolocatorLocationProvider implements LocationProvider {
  /// How long to wait for a fix before giving up.
  ///
  /// Bounded because a punch cannot wait indefinitely. When it expires the app
  /// sends without a fresh fix rather than blocking the employee — the
  /// alternative would be a registration the client refused, which Art. 78
  /// forbids.
  static const Duration _timeLimit = Duration(seconds: 12);

  @override
  Future<LocationPermissionState> checkPermission() async =>
      _translate(await Geolocator.checkPermission());

  @override
  Future<LocationPermissionState> requestPermission() async =>
      _translate(await Geolocator.requestPermission());

  @override
  Future<bool> isServiceEnabled() => Geolocator.isLocationServiceEnabled();

  @override
  Future<CapturedLocation> currentLocation() async {
    final position = await Geolocator.getCurrentPosition(
      locationSettings: const LocationSettings(
        accuracy: LocationAccuracy.high,
        timeLimit: _timeLimit,
      ),
    );

    return CapturedLocation(
      latitude: position.latitude,
      longitude: position.longitude,
      accuracyMeters: position.accuracy,
      capturedAt: position.timestamp,

      // Android reports this when the fix came from a mock provider. It catches
      // the easy path — a fake GPS app takes two minutes to install — and not a
      // rooted device with the right tooling.
      isMocked: position.isMocked,
    );
  }

  static LocationPermissionState _translate(LocationPermission permission) =>
      switch (permission) {
        LocationPermission.always ||
        LocationPermission.whileInUse => LocationPermissionState.granted,
        LocationPermission.deniedForever =>
          LocationPermissionState.deniedForever,
        LocationPermission.denied ||
        LocationPermission.unableToDetermine => LocationPermissionState.denied,
      };
}
