import 'package:nahora/features/attendance/domain/captured_location.dart';
import 'package:nahora/features/attendance/domain/location_provider.dart';

/// A device whose location behaviour a test decides.
///
/// Shared so the punch controller and the screens that show its state agree
/// on what a granted permission and a good fix look like.
class FakeLocationProvider implements LocationProvider {
  LocationPermissionState permission = LocationPermissionState.granted;
  LocationPermissionState? permissionAfterRequest;
  bool serviceEnabled = true;
  bool failsToLocate = false;
  CapturedLocation? position;

  int requestCount = 0;

  @override
  Future<LocationPermissionState> checkPermission() async => permission;

  @override
  Future<LocationPermissionState> requestPermission() async {
    requestCount++;

    return permissionAfterRequest ?? permission;
  }

  @override
  Future<bool> isServiceEnabled() async => serviceEnabled;

  @override
  Future<CapturedLocation> currentLocation() async {
    if (failsToLocate || position == null) {
      throw StateError('no fix');
    }

    return position!;
  }
}
