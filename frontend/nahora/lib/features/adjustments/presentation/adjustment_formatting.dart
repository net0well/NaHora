import '../../../design_system/components/app_badge_tone.dart';
import '../domain/adjustment.dart';
import '../domain/adjustment_status.dart';

/// How a correction is rendered wherever one is listed.
///
/// Every time goes through the company offset the server sent with the record,
/// never the device's own: a manager approving from another timezone must read
/// the same hour their employee typed.
extension AdjustmentFormatting on Adjustment {
  /// "30/08/2026" — the day the corrected punch belongs to.
  String get dayLabel {
    final local = adjustedOccurredAt.add(companyOffset);

    return '${_pad(local.day)}/${_pad(local.month)}/${local.year}';
  }

  /// "08:05" — the time being asked for.
  String get adjustedTimeLabel => _time(adjustedOccurredAt);

  /// "08:32" — the time it replaces, or null for an inclusion.
  String? get originalTimeLabel {
    final original = originalOccurredAt;

    return original == null ? null : _time(original);
  }

  /// What is actually changing, in one line.
  ///
  /// An inclusion has no original to name, and printing "de — para 08:05"
  /// would invite somebody to read the dash as a missing value.
  String get changeLabel {
    final original = originalTimeLabel;

    return original == null
        ? 'Incluir às $adjustedTimeLabel'
        : 'De $original para $adjustedTimeLabel';
  }

  /// "30/08 às 09:12" — when the request arrived.
  String get requestedAtLabel {
    final local = requestedAt.add(companyOffset);

    return '${_pad(local.day)}/${_pad(local.month)} às ${_time(requestedAt)}';
  }

  /// "Correção de horário · Entrada"
  String get subjectLabel => '${kind.label} · ${direction.label}';

  /// Colour is never alone: [AdjustmentStatus.label] carries the same meaning
  /// in text, and the badge renders an icon per tone.
  AppBadgeTone get statusTone => switch (status) {
    AdjustmentStatus.pending => AppBadgeTone.warning,
    AdjustmentStatus.approved => AppBadgeTone.success,
    AdjustmentStatus.rejected => AppBadgeTone.neutral,
  };

  String _time(DateTime instant) {
    final local = instant.add(companyOffset);

    return '${_pad(local.hour)}:${_pad(local.minute)}';
  }
}

String _pad(int value) => value.toString().padLeft(2, '0');
