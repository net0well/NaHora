/// The official time, kept without trusting the device clock.
///
/// The rule is that a punch is stamped by the server, so the employee cannot
/// punch early by moving the phone forward. The screen has to obey the same
/// rule: showing 16:32 while recording 14:32 would make the app look like it
/// lied.
///
/// So the clock is anchored. On sync it records the server's instant alongside
/// a **monotonic** reading — a stopwatch counting since the app started, which
/// changing the date does not affect. Every later reading is the anchor plus
/// however much that stopwatch has advanced.
///
/// This is also what makes an offline punch possible: the same anchor produces
/// an estimate when there is no server to ask.
class ServerClock {
  ServerClock({Stopwatch? monotonic})
    : _monotonic = (monotonic ?? Stopwatch())..start();

  final Stopwatch _monotonic;

  DateTime? _anchorServerTime;
  Duration? _anchorElapsed;

  /// True once the clock has something real to count from.
  ///
  /// Until then the app has no honest time to show, and the screen says so
  /// rather than falling back to the device — falling back is exactly the bug
  /// this class exists to prevent.
  bool get isSynchronised => _anchorServerTime != null;

  /// Records the server's instant against the current monotonic reading.
  void synchronise(DateTime serverTimeUtc) {
    _anchorServerTime = serverTimeUtc.toUtc();
    _anchorElapsed = _monotonic.elapsed;
  }

  /// The monotonic reading now, in milliseconds.
  ///
  /// Travels with a queued punch so the estimate can be audited rather than
  /// merely believed: the anchor plus this is exactly how the app arrived at
  /// the time it claims.
  int get monotonicElapsedMs => _monotonic.elapsed.inMilliseconds;

  /// The server instant this clock counts from, or null while unsynchronised.
  DateTime? get anchorAt => _anchorServerTime;

  /// The official time now, or null while unsynchronised.
  DateTime? nowUtc() {
    final anchor = _anchorServerTime;
    final anchoredAt = _anchorElapsed;

    if (anchor == null || anchoredAt == null) {
      return null;
    }

    return anchor.add(_monotonic.elapsed - anchoredAt);
  }

  /// How far the device clock sits from the official time.
  ///
  /// Sent with the punch as evidence. A phone a couple of minutes out is
  /// ordinary; hours out is the signal that someone moved it deliberately.
  Duration? deviceSkew() {
    final official = nowUtc();

    return official == null
        ? null
        : DateTime.now().toUtc().difference(official);
  }
}
