import '../../../design_system/components/status_tone.dart';
import '../domain/punch_entry.dart';
import '../domain/punch_trust.dart';

/// How a mark is rendered in a list.
///
/// Shared by the day list and the history so a punch does not read as trusted
/// on one screen and degraded on the other.
extension PunchEntryFormatting on PunchEntry {
  /// The time in company hours, from the offset the server sent with this mark.
  ///
  /// Never the device's own timezone: a phone set to another one would show the
  /// employee a time that does not match their record.
  String get timeLabel => _asTime(occurredAt);

  /// The time a correction replaced, or null when nothing was replaced.
  String? get originalTimeLabel {
    final original = originalOccurredAt;

    return original == null ? null : _asTime(original);
  }

  /// Colour is never alone: [note] carries the same meaning in text, and the
  /// tile also renders an icon per tone.
  StatusTone get tone {
    // An inclusion was never measured, so it has no capture to be confident or
    // doubtful about. It reads as a plain statement rather than as a verdict.
    if (isIncluded) {
      return StatusTone.neutral;
    }

    return switch (trust) {
      PunchTrust.degraded => StatusTone.warning,
      PunchTrust.needsReview => StatusTone.danger,
      _ => StatusTone.success,
    };
  }

  /// What the row says under the direction, or null when there is nothing worth
  /// adding — a trusted online punch needs no explanation.
  ///
  /// An adjustment always says so, and says what it replaced. Somebody looking
  /// at their own hours has the right to notice that a time was changed.
  String? get note {
    if (isIncluded) {
      return 'Incluída por ajuste';
    }

    if (isAdjusted) {
      final original = originalTimeLabel;

      return original == null ? 'Horário ajustado' : 'Ajustada de $original';
    }

    if (trust == PunchTrust.trusted && !isOffline) {
      return null;
    }

    if (isOffline && trust != PunchTrust.needsReview) {
      return 'Registrada offline';
    }

    return trust?.label;
  }

  String _asTime(DateTime instant) {
    final local = instant.add(companyOffset);

    return '${_pad(local.hour)}:${_pad(local.minute)}';
  }
}

String _pad(int value) => value.toString().padLeft(2, '0');
