import '../domain/captured_location.dart';
import '../domain/punch_receipt.dart';

/// Where the punch screen stands.
///
/// Two of these carry the whole rule of the product, and neither disables the
/// button: [PunchLowAccuracy] and [PunchLocationUnavailable]. Portaria 671,
/// Art. 78 forbids the registrar from restricting the record, and bad signal is
/// not an exception — a warehouse degrades the fix and the employee still has
/// the right to clock in.
sealed class PunchState {
  const PunchState();

  /// Whether the employee can press the button.
  ///
  /// Deliberately true for the two states that look like failures. Only a
  /// missing permission and a request already in flight stop it.
  bool get canPunch => switch (this) {
    PunchReady() || PunchLowAccuracy() || PunchLocationUnavailable() => true,
    _ => false,
  };
}

/// Waiting for the first fix.
class PunchLocating extends PunchState {
  const PunchLocating();
}

/// Located, precise enough, ready to go.
class PunchReady extends PunchState {
  const PunchReady(this.location);

  final CapturedLocation location;
}

/// Located, but the radius is too wide to narrow anything useful.
class PunchLowAccuracy extends PunchState {
  const PunchLowAccuracy(this.location);

  final CapturedLocation location;
}

/// No fix at all — indoors, or the location service is off.
///
/// Still punchable. The punch goes without a position and the server records
/// what it got.
class PunchLocationUnavailable extends PunchState {
  const PunchLocationUnavailable(this.reason);

  final String reason;
}

/// The app has no permission to read the position.
///
/// The one state that blocks, and it blocks because there is nothing to send:
/// without permission there is no position, and the punch would carry none.
class PunchPermissionDenied extends PunchState {
  const PunchPermissionDenied({required this.isPermanent});

  /// Refused permanently. Only the Android settings screen can undo it, so the
  /// message has to say that rather than offering a button that does nothing.
  final bool isPermanent;
}

/// Sending.
class PunchSubmitting extends PunchState {
  const PunchSubmitting();
}

/// Recorded, and the server said so.
class PunchConfirmed extends PunchState {
  const PunchConfirmed(this.receipt);

  final PunchReceipt receipt;
}

/// The attempt failed and the employee can try again.
class PunchFailed extends PunchState {
  const PunchFailed(this.message);

  /// Already translated. The screen renders it and decides nothing.
  final String message;

  @override
  bool get canPunch => true;
}

/// Recorded on the device because there was no signal.
///
/// Not a failure. The mark exists, it keeps the time it was made at, and it
/// goes up on its own when the network comes back — which is the whole point of
/// a client that may never refuse a registration.
class PunchQueued extends PunchState {
  const PunchQueued(this.pendingCount);

  /// How many are waiting, this one included.
  final int pendingCount;

  @override
  bool get canPunch => true;
}
