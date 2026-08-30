import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/errors/app_exception.dart';
import '../../attendance/presentation/attendance_providers.dart';
import 'adjustments_providers.dart';
import 'decision_state.dart';

/// Approves and refuses requests from the manager's queue.
///
/// Holds no `BuildContext`, so a decision can be driven in a test without
/// rendering the queue.
class DecisionController extends AutoDisposeNotifier<DecisionState> {
  @override
  DecisionState build() => const DecisionState();

  /// Decides one request and refreshes what the decision changed.
  ///
  /// An approval alters the record the manager is also an employee of, so the
  /// day and the history are invalidated alongside the queue. Leaving them
  /// would show the old times until something else happened to reload them.
  Future<void> decide({
    required String adjustmentId,
    required bool approve,
    String? notes,
  }) async {
    if (state.isBusy) {
      return;
    }

    state = DecisionState(decidingId: adjustmentId);

    try {
      await ref
          .read(adjustmentsRepositoryProvider)
          .decide(adjustmentId: adjustmentId, approve: approve, notes: notes);

      ref
        ..invalidate(pendingAdjustmentsProvider)
        ..invalidate(punchDayProvider)
        ..invalidate(punchHistoryProvider);

      state = const DecisionState();
    } on AppException catch (exception) {
      state = DecisionState(
        errorMessage: ErrorMessages.resolve(exception.code),
      );
    }
  }

  /// Drops the failure so the banner does not outlive the next attempt.
  void dismissError() => state = DecisionState(decidingId: state.decidingId);
}

final decisionControllerProvider =
    AutoDisposeNotifierProvider<DecisionController, DecisionState>(
      DecisionController.new,
    );
