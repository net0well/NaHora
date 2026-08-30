import 'captured_location.dart';

/// Reads the device's position.
///
/// A port, so a test can hand the controller a position without a GPS. The
/// concrete implementation is named for the package it adapts.
abstract interface class LocationProvider {
  /// Whether the app may read the position at all.
  Future<LocationPermissionState> checkPermission();

  /// Asks the user. Returns what they chose.
  Future<LocationPermissionState> requestPermission();

  /// Whether the device's location service is switched on.
  Future<bool> isServiceEnabled();

  /// Reads a position, or throws if none can be obtained.
  Future<CapturedLocation> currentLocation();
}

/// What the platform will let the app do.
enum LocationPermissionState {
  granted,

  /// Refused, but the user can be asked again.
  denied,

  /// Refused permanently. Only the Android settings screen can undo it, which
  /// is where people give up — so the app must explain before asking.
  deniedForever,
}
