import 'package:flutter_test/flutter_test.dart';
import 'package:nahora/features/attendance/data/server_clock.dart';

/// The clock that must not be foolable by changing the phone's date.
void main() {
  group('ServerClock', () {
    test(
      'Given no synchronisation, When read, Then it admits it has no time',
      () {
        // Arrange — the alternative would be falling back to the device clock,
        // which is the exact failure this class exists to prevent
        final clock = ServerClock();

        // Assert
        expect(clock.isSynchronised, isFalse);
        expect(clock.nowUtc(), isNull);
      },
    );

    test('Given a synchronisation, When read immediately, Then it returns the server time', () {
      // Arrange
      final serverTime = DateTime.utc(2026, 8, 30, 14, 32);
      final clock = ServerClock();

      // Act
      clock.synchronise(serverTime);

      // Assert
      expect(clock.isSynchronised, isTrue);
      expect(
        clock.nowUtc()!.difference(serverTime).inSeconds,
        0,
        reason: 'no measurable time has passed since the anchor',
      );
    });

    test('Given time passing, When read again, Then it advances by the elapsed amount', () {
      // Arrange — a controllable stopwatch stands in for the monotonic
      // source, so the assertion is exact rather than timing-dependent
      final monotonic = _FakeStopwatch();
      final clock = ServerClock(monotonic: monotonic)
        ..synchronise(DateTime.utc(2026, 8, 30, 14, 32));

      // Act
      monotonic.advance(const Duration(minutes: 5, seconds: 30));

      // Assert
      expect(clock.nowUtc(), DateTime.utc(2026, 8, 30, 14, 37, 30));
    });

    test('Given the device clock is hours ahead, When the skew is read, '
        'Then it reports the difference without adopting it', () {
      // Arrange — this is the whole point: the employee moves the phone
      // forward, and the app keeps counting from the server's anchor while
      // recording how far off the device is
      final monotonic = _FakeStopwatch();
      final clock = ServerClock(
        monotonic: monotonic,
      )..synchronise(DateTime.now().toUtc().subtract(const Duration(hours: 3)));

      // Act
      final skew = clock.deviceSkew();

      // Assert — the official time stayed three hours behind the device
      expect(skew!.inMinutes, closeTo(180, 1));
      expect(
        clock.nowUtc()!.isBefore(DateTime.now().toUtc()),
        isTrue,
        reason: 'the anchor wins, not the device',
      );
    });

    test(
      'Given a resynchronisation, When it happens, Then the anchor moves',
      () {
        // Arrange
        final monotonic = _FakeStopwatch();
        final clock = ServerClock(monotonic: monotonic)
          ..synchronise(DateTime.utc(2026, 8, 30, 10));

        monotonic.advance(const Duration(hours: 1));

        // Act — the server is the authority, so a fresh reading replaces the
        // accumulated estimate rather than being averaged with it
        clock.synchronise(DateTime.utc(2026, 8, 30, 20));

        // Assert
        expect(clock.nowUtc(), DateTime.utc(2026, 8, 30, 20));
      },
    );
  });
}

/// A stopwatch whose elapsed time the test decides.
class _FakeStopwatch implements Stopwatch {
  Duration _elapsed = Duration.zero;

  void advance(Duration amount) => _elapsed += amount;

  @override
  Duration get elapsed => _elapsed;

  @override
  void start() {}

  @override
  void stop() {}

  @override
  void reset() => _elapsed = Duration.zero;

  @override
  int get elapsedMicroseconds => _elapsed.inMicroseconds;

  @override
  int get elapsedMilliseconds => _elapsed.inMilliseconds;

  @override
  int get elapsedTicks => _elapsed.inMicroseconds;

  @override
  int get frequency => 1000000;

  @override
  bool get isRunning => true;
}
