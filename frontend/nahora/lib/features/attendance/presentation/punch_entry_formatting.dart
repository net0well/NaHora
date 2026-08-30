import '../../../design_system/components/status_tone.dart';
import '../domain/punch_entry.dart';
import '../domain/punch_trust.dart';

/// How a recorded punch is rendered in a list.
///
/// Shared by the day list and the history so a punch does not read as trusted
/// on one screen and degraded on the other.
extension PunchEntryFormatting on PunchEntry {
  /// The time in company hours, from the offset the server sent with this
  /// punch.
  ///
  /// Never the device's own timezone: a phone set to another one would show the
  /// employee a time that does not match their record.
  String get timeLabel {
    final local = occurredAt.add(companyOffset);

    return '${_pad(local.hour)}:${_pad(local.minute)}';
  }

  /// Colour is never alone: [PunchTrust.label] carries the same meaning in
  /// text, and the tile also renders an icon per tone.
  StatusTone get tone => switch (trust) {
    PunchTrust.trusted => StatusTone.success,
    PunchTrust.degraded => StatusTone.warning,
    PunchTrust.needsReview => StatusTone.danger,
  };

  /// What the row says under the direction, or null when there is nothing worth
  /// adding — a trusted online punch needs no explanation.
  String? get note {
    if (trust == PunchTrust.trusted && !isOffline) {
      return null;
    }

    if (isOffline && trust != PunchTrust.needsReview) {
      return 'Registrada offline';
    }

    return trust.label;
  }
}

String _pad(int value) => value.toString().padLeft(2, '0');
