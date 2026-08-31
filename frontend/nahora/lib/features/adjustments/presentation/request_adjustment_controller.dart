import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/errors/app_exception.dart';
import '../../attendance/presentation/attendance_providers.dart';
import 'adjustments_providers.dart';
import 'request_adjustment_state.dart';

/// Sends a correction request, and its document if there is one.
///
/// Holds no `BuildContext`, so the whole submission can be driven in a test —
/// including the case that only happens on bad signal, where the request lands
/// and the upload does not.
class RequestAdjustmentController
    extends AutoDisposeNotifier<RequestAdjustmentState> {
  @override
  RequestAdjustmentState build() => const RequestAdjustmentState();

  Future<void> submit({
    required String justificationId,
    required DateTime occurredAt,
    String? punchId,
    String? direction,
    String? notes,
    String? attachmentPath,
  }) async {
    if (state.isSubmitting) {
      return;
    }

    state = const RequestAdjustmentState(isSubmitting: true);

    final repository = ref.read(adjustmentsRepositoryProvider);

    try {
      final adjustment = await repository.request(
        justificationId: justificationId,
        occurredAt: occurredAt,
        punchId: punchId,
        direction: direction,
        notes: notes,
      );

      // The document goes second, and its failure does not undo the first
      // call. The request is the record with legal weight; a photo that did
      // not upload is something to send again, not a reason to pretend nobody
      // asked.
      final attachmentFailed = attachmentPath == null
          ? false
          : !await _attach(adjustment.id, attachmentPath);

      // The employee's own pending list changes, and so does the manager's
      // queue if they happen to be the same person.
      ref
        ..invalidate(pendingAdjustmentsProvider)
        ..invalidate(punchDayProvider)
        ..invalidate(punchHistoryProvider);

      state = RequestAdjustmentState(
        isSent: true,
        attachmentFailed: attachmentFailed,
      );
    } on AppException catch (exception) {
      state = RequestAdjustmentState(
        errorMessage: ErrorMessages.resolve(exception.code),
      );
    }
  }

  Future<bool> _attach(String adjustmentId, String path) async {
    try {
      await ref
          .read(adjustmentsRepositoryProvider)
          .attach(adjustmentId: adjustmentId, filePath: path);

      return true;
    } on AppException {
      return false;
    }
  }
}

final requestAdjustmentControllerProvider =
    AutoDisposeNotifierProvider<
      RequestAdjustmentController,
      RequestAdjustmentState
    >(RequestAdjustmentController.new);
