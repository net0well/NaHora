/// Where the time that counts came from.
///
/// The employee is entitled to know which of their marks were touched. A
/// corrected time drawn exactly like a captured one hides a decision inside
/// their own record.
enum PunchEntrySource {
  /// Captured by the registrar and never corrected.
  recorded('Recorded'),

  /// Captured, then given a different time by an approved adjustment.
  adjusted('Adjusted'),

  /// Never captured. It counts because an adjustment was approved.
  included('Included');

  const PunchEntrySource(this.code);

  /// What the API sends. The contract, not the display text.
  final String code;

  static PunchEntrySource fromCode(String? code) => values.firstWhere(
    (source) => source.code == code,
    orElse: () => PunchEntrySource.recorded,
  );
}
