import '../../attendance/domain/punch_direction.dart';
import 'adjustment_kind.dart';
import 'adjustment_status.dart';

/// A request to correct the record, as both sides of it read it.
///
/// The employee sees their own; the manager sees the company's. It is the same
/// object either way — a correction that looked different depending on who
/// opened it would be two records pretending to be one.
class Adjustment {
  const Adjustment({
    required this.id,
    required this.employeeId,
    required this.employeeName,
    required this.kind,
    required this.status,
    required this.counts,
    required this.direction,
    required this.adjustedOccurredAt,
    required this.companyOffset,
    required this.justificationCode,
    required this.justificationDescription,
    required this.hasAttachment,
    required this.requestedAt,
    required this.requestedByName,
    this.originalOccurredAt,
    this.notes,
    this.decidedAt,
    this.decisionNotes,
  });

  factory Adjustment.fromJson(Map<String, dynamic> json) => Adjustment(
    id: json['id'] as String,
    employeeId: json['employeeId'] as String,
    employeeName: json['employeeName'] as String,
    kind: AdjustmentKind.fromCode(json['kind'] as String),
    status: AdjustmentStatus.fromCode(json['status'] as String),
    counts: (json['counts'] as bool?) ?? false,
    direction: PunchDirection.fromCode(json['direction'] as String),
    originalOccurredAt: _parse(json['originalOccurredAt']),
    adjustedOccurredAt: DateTime.parse(json['adjustedOccurredAt'] as String)
        .toUtc(),
    companyOffset: Duration(seconds: (json['utcOffsetSeconds'] as int?) ?? 0),
    justificationCode: json['justificationCode'] as String,
    justificationDescription: json['justificationDescription'] as String,
    notes: json['notes'] as String?,
    hasAttachment: (json['hasAttachment'] as bool?) ?? false,
    requestedAt: DateTime.parse(json['requestedAt'] as String).toUtc(),
    requestedByName: json['requestedByName'] as String,
    decidedAt: _parse(json['decidedAt']),
    decisionNotes: json['decisionNotes'] as String?,
  );

  final String id;

  final String employeeId;

  /// Whose record this corrects. The manager's queue is a list of people before
  /// it is a list of times.
  final String employeeName;

  final AdjustmentKind kind;

  final AdjustmentStatus status;

  /// Whether this is the time that now governs. Only an approved one does.
  final bool counts;

  final PunchDirection direction;

  /// The time being replaced, in UTC. Null for an inclusion, which replaces
  /// nothing.
  ///
  /// Kept beside the new value rather than overwritten: a correction that hides
  /// what it replaced is an edit, and spec 014 requires the original to stay
  /// visible.
  final DateTime? originalOccurredAt;

  /// The time asked for, in UTC.
  final DateTime adjustedOccurredAt;

  /// The company offset in force for this correction, sent by the server.
  ///
  /// Never the device's own: a manager travelling would otherwise approve a
  /// time that reads differently from the one their employee asked for.
  final Duration companyOffset;

  /// The stable code from the catalogue. The description can be rewritten; this
  /// cannot.
  final String justificationCode;

  final String justificationDescription;

  /// What the employee wrote to explain the request.
  final String? notes;

  /// Whether a file was attached, never where it is. The link is fetched
  /// through its own authorised route.
  final bool hasAttachment;

  final DateTime requestedAt;

  final String requestedByName;

  final DateTime? decidedAt;

  /// What the manager wrote when deciding.
  final String? decisionNotes;

  bool get isPending => status == AdjustmentStatus.pending;

  static DateTime? _parse(Object? value) =>
      value is String ? DateTime.parse(value).toUtc() : null;
}
