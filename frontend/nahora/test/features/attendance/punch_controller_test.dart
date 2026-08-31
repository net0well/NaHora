import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:nahora/core/errors/app_exception.dart';
import 'package:nahora/features/attendance/domain/captured_location.dart';
import 'package:nahora/features/attendance/domain/location_provider.dart';
import 'package:nahora/features/attendance/presentation/attendance_providers.dart';
import 'package:nahora/features/attendance/presentation/punch_state.dart';

import '../../support/fake_attendance_repository.dart';
import '../../support/fake_location_provider.dart';
import '../../support/fake_punch_queue.dart';

/// The state machine behind the punch button.
///
/// Worth testing here rather than on a device, because the interesting states —
/// permission refused for good, a mocked position, a fix too wide to be useful —
/// are exactly the ones a real GPS will not produce on demand.
void main() {
  group('PunchController', () {
    late FakeLocationProvider location;
    late FakeAttendanceRepository repository;
    late FakePunchQueue queue;

    setUp(() {
      location = FakeLocationProvider();
      repository = FakeAttendanceRepository();
      queue = FakePunchQueue();
    });

    ProviderContainer buildContainer() {
      final container = ProviderContainer(
        overrides: [
          locationProviderProvider.overrideWithValue(location),
          attendanceRepositoryProvider.overrideWithValue(repository),
          deviceIdProvider.overrideWith((ref) async => 'test-device'),
          clientIdFactoryProvider.overrideWithValue(() => 'client-fixed'),
          punchQueueProvider.overrideWithValue(queue),
        ],
      );
      addTearDown(container.dispose);

      return container;
    }

    Future<PunchState> settled(ProviderContainer container) async {
      container.listen(punchControllerProvider, (_, _) {});
      await Future<void>.delayed(Duration.zero);

      return container.read(punchControllerProvider);
    }

    test('Given a precise fix, When the screen prepares, Then it is ready to punch', () async {
      // Arrange
      location.position = _locationWith(accuracyMeters: 12);

      // Act
      final state = await settled(buildContainer());

      // Assert
      expect(state, isA<PunchReady>());
      expect(state.canPunch, isTrue);
    });

    test('Given a fix too wide to be useful, When prepared, '
        'Then it warns and still allows the punch', () async {
      // Arrange — inside a warehouse the radius blows out, and Art. 78
      // forbids the client from refusing the registration over it
      location.position = _locationWith(accuracyMeters: 480);

      // Act
      final state = await settled(buildContainer());

      // Assert
      expect(state, isA<PunchLowAccuracy>());
      expect(
        state.canPunch,
        isTrue,
        reason: 'bad signal must never block a punch',
      );
    });

    test(
      'Given no fix at all, When prepared, Then it still allows the punch',
      () async {
        // Arrange
        location.failsToLocate = true;

        // Act
        final state = await settled(buildContainer());

        // Assert
        expect(state, isA<PunchLocationUnavailable>());
        expect(state.canPunch, isTrue);
      },
    );

    test('Given the location service is off, When prepared, Then it says so and allows the punch', () async {
      // Arrange
      location.serviceEnabled = false;

      // Act
      final state = await settled(buildContainer());

      // Assert
      expect(state, isA<PunchLocationUnavailable>());
      expect(state.canPunch, isTrue);
    });

    test('Given permission refused for good, When prepared, Then the button is blocked', () async {
      // Arrange — the one case that blocks, and it blocks because without
      // permission there is no position to send at all
      location.permission = LocationPermissionState.deniedForever;

      // Act
      final state = await settled(buildContainer());

      // Assert
      expect(state, isA<PunchPermissionDenied>());
      expect((state as PunchPermissionDenied).isPermanent, isTrue);
      expect(state.canPunch, isFalse);
    });

    test(
      'Given permission not yet asked, When prepared, Then it asks once',
      () async {
        // Arrange
        location.permission = LocationPermissionState.denied;
        location.permissionAfterRequest = LocationPermissionState.granted;
        location.position = _locationWith(accuracyMeters: 10);

        // Act
        final state = await settled(buildContainer());

        // Assert
        expect(location.requestCount, 1);
        expect(state, isA<PunchReady>());
      },
    );

    test('Given a mocked position, When punched, Then it is sent rather than refused', () async {
      // Arrange — the client reports the flag; the server decides what it
      // means. A client that refused here would be the restriction Art. 78
      // forbids.
      location.position = _locationWith(accuracyMeters: 10, isMocked: true);
      final container = buildContainer();
      await settled(container);

      // Act
      await container.read(punchControllerProvider.notifier).punch();

      // Assert
      expect(repository.sentLocations.single.isMocked, isTrue);
      expect(container.read(punchControllerProvider), isA<PunchConfirmed>());
    });

    test('Given the screen has been open a while, When punched, '
        'Then the position is read again before sending', () async {
      // Arrange — the fix drawn on the map can be minutes old by the time a
      // thumb finishes the hold, and what counts is where the person was when
      // they marked
      location.position = _locationWith(accuracyMeters: 10);
      final container = buildContainer();
      await settled(container);

      final onOpening = location.handedOut.length;
      location.position = _locationWith(accuracyMeters: 4);

      // Act
      await container.read(punchControllerProvider.notifier).punch();

      // Assert
      expect(location.handedOut.length, onOpening + 1);
      expect(repository.sentLocations.single.accuracyMeters, 4);
    });

    test('Given the second reading fails, When punched, '
        'Then it goes with the one already on screen', () async {
      // Arrange — a failed re-read is not a reason to refuse. Nothing on the
      // client may stop a registration: Art. 78 §1º.
      location.position = _locationWith(accuracyMeters: 10);
      final container = buildContainer();
      await settled(container);

      location.failsToLocate = true;

      // Act
      await container.read(punchControllerProvider.notifier).punch();

      // Assert
      expect(repository.sentLocations.single.accuracyMeters, 10);
      expect(container.read(punchControllerProvider), isA<PunchConfirmed>());
    });

    test('Given no network, When punched, '
        'Then the mark is kept on the device rather than refused', () async {
      // Arrange — a client that answered "try later" would be refusing a
      // registration, which Art. 78 §1º does not allow it to do
      location.position = _locationWith(accuracyMeters: 10);
      final container = buildContainer();
      await settled(container);

      repository.failure = const NetworkException();

      // Act
      await container.read(punchControllerProvider.notifier).punch();

      // Assert
      final state = container.read(punchControllerProvider);
      expect(state, isA<PunchQueued>());
      expect((state as PunchQueued).pendingCount, 1);
      expect(queue.held.single.clientId, 'client-fixed');
      expect(queue.held.single.latitude, -23.5505);
    });

    test('Given a queued punch, When the next one succeeds, '
        'Then the held one goes up with the time it was made at', () async {
      // Arrange — stamping arrival time would record a punch made at 07:00 as
      // whenever signal came back
      location.position = _locationWith(accuracyMeters: 10);
      final container = buildContainer();
      await settled(container);

      repository.failure = const NetworkException();
      await container.read(punchControllerProvider.notifier).punch();

      final made = queue.held.single.occurredAtEstimate;
      repository.failure = null;

      // Act
      await container.read(punchControllerProvider.notifier).punch();

      // Assert — the live punch, then the one that was waiting
      expect(queue.held, isEmpty);
      expect(repository.sentOffline.length, 2);
      expect(repository.sentOffline.first, isNull);
      expect(repository.sentOffline.last?.occurredAt, made);
    });

    test('Given a punch refused on its merits, When drained, '
        'Then it is dropped instead of blocking the queue', () async {
      // Arrange — kept, it would fail on every drain from now on and hold every
      // later mark behind it
      location.position = _locationWith(accuracyMeters: 10);
      final container = buildContainer();
      await settled(container);

      repository.failure = const NetworkException();
      await container.read(punchControllerProvider.notifier).punch();

      repository.failure = const BusinessException('PUNCH_ALREADY_REGISTERED');

      // Act
      await container.read(punchSyncProvider).drain();

      // Assert
      expect(queue.held, isEmpty);
    });
    test(
      'Given a punch, When sent, Then no time is sent as the official one',
      () async {
        // Arrange — a client that could send the time would let someone punch
        // early by changing the phone clock
        location.position = _locationWith(accuracyMeters: 10);
        final container = buildContainer();
        await settled(container);

        // Act
        await container.read(punchControllerProvider.notifier).punch();

        // Assert — the device clock travels only as evidence, and the recorded
        // time comes back from the server
        expect(repository.sentDeviceClocks, hasLength(1));
        final confirmed =
            container.read(punchControllerProvider) as PunchConfirmed;
        expect(
          confirmed.receipt.occurredAt,
          FakeAttendanceRepository.serverStamp,
        );
      },
    );

    test(
      'Given the request fails, When it does, Then the message is translated '
      'and the button works again',
      () async {
        // Arrange
        location.position = _locationWith(accuracyMeters: 10);
        repository.failure = const BusinessException(
          'EMPLOYEE_RECORD_REQUIRED',
        );

        final container = buildContainer();
        await settled(container);

        // Act
        await container.read(punchControllerProvider.notifier).punch();

        // Assert
        final state = container.read(punchControllerProvider);
        expect(state, isA<PunchFailed>());
        expect(
          (state as PunchFailed).message,
          isNot(contains('EMPLOYEE_RECORD_REQUIRED')),
        );
        expect(
          state.canPunch,
          isTrue,
          reason: 'a failure must not trap the employee',
        );
      },
    );

    test('Given a punch in flight, When the button is pressed again, Then nothing is sent twice', () async {
      // Arrange
      location.position = _locationWith(accuracyMeters: 10);
      final container = buildContainer();
      await settled(container);

      // Act
      final first = container.read(punchControllerProvider.notifier).punch();
      await container.read(punchControllerProvider.notifier).punch();
      await first;

      // Assert
      expect(repository.sentLocations, hasLength(1));
    });
  });
}

CapturedLocation _locationWith({
  required double accuracyMeters,
  bool isMocked = false,
}) => CapturedLocation(
  latitude: -23.5505,
  longitude: -46.6333,
  accuracyMeters: accuracyMeters,
  capturedAt: DateTime.utc(2026, 8, 30, 14, 32),
  isMocked: isMocked,
);
