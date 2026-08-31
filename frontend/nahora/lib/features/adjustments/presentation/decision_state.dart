/// What the manager's queue is doing while a decision is in flight.
///
/// The identifier rather than a plain flag: the queue is a list, and a single
/// boolean would spin every row when only one of them was sent.
class DecisionState {
  const DecisionState({this.decidingId, this.errorMessage});

  /// The request currently being decided, or null when none is.
  final String? decidingId;

  /// What went wrong, already translated. Null when nothing did.
  final String? errorMessage;

  bool isDeciding(String adjustmentId) => decidingId == adjustmentId;

  bool get isBusy => decidingId != null;
}
