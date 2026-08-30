import 'package:nahora/core/errors/app_exception.dart';
import 'package:nahora/features/adjustments/data/adjustments_repository.dart';
import 'package:nahora/features/adjustments/domain/adjustment.dart';
import 'package:nahora/features/adjustments/domain/adjustment_kind.dart';
import 'package:nahora/features/adjustments/domain/adjustment_status.dart';
import 'package:nahora/features/attendance/domain/punch_direction.dart';

/// A stand-in for the adjustment endpoints.
class FakeAdjustmentsRepository implements AdjustmentsRepository {
  /// What the queue reads back.
  List<Adjustment> requests = [];

  /// Thrown by [list] when set.
  AppException? failure;

  /// Thrown by [decide] when set.
  AppException? decisionFailure;

  /// Every decision sent, in order — enough to assert that Recusar did not
  /// quietly approve.
  final List<({String adjustmentId, bool approve})> decisions = [];

  /// Held open when set, so a test can inspect the screen mid-request.
  Future<void>? decisionGate;

  @override
  Future<List<Adjustment>> list({bool pendingOnly = false}) async {
    if (failure != null) {
      throw failure!;
    }

    return pendingOnly
        ? requests.where((request) => request.isPending).toList()
        : requests;
  }

  @override
  Future<Adjustment> decide({
    required String adjustmentId,
    required bool approve,
    String? notes,
  }) async {
    decisions.add((adjustmentId: adjustmentId, approve: approve));

    if (decisionGate != null) {
      await decisionGate;
    }

    if (decisionFailure != null) {
      throw decisionFailure!;
    }

    final decided = requests.firstWhere(
      (request) => request.id == adjustmentId,
    );

    requests = requests.where((request) => request.id != adjustmentId).toList();

    return decided;
  }

  /// One pending request, with the fields a queue actually renders.
  ///
  /// Times are UTC, as the server sends them. The offset is São Paulo's, so a
  /// test asserting "08:05" is asserting the conversion and not a coincidence.
  static Adjustment pending({
    String id = 'adjustment-1',
    String employeeName = 'Joana Funcionária',
    AdjustmentKind kind = AdjustmentKind.timeChange,
    DateTime? originalOccurredAt,
    DateTime? adjustedOccurredAt,
    String? notes,
    bool hasAttachment = false,
  }) => Adjustment(
    id: id,
    employeeId: 'employee-1',
    employeeName: employeeName,
    kind: kind,
    status: AdjustmentStatus.pending,
    counts: false,
    direction: PunchDirection.entry,
    originalOccurredAt:
        originalOccurredAt ??
        (kind == AdjustmentKind.timeChange
            ? DateTime.utc(2026, 8, 30, 11, 32)
            : null),
    adjustedOccurredAt: adjustedOccurredAt ?? DateTime.utc(2026, 8, 30, 11, 5),
    companyOffset: const Duration(hours: -3),
    justificationCode: 'FORGOT',
    justificationDescription: 'Esquecimento de marcação',
    notes: notes,
    hasAttachment: hasAttachment,
    requestedAt: DateTime.utc(2026, 8, 30, 12, 12),
    requestedByName: 'Joana Funcionária',
  );
}
