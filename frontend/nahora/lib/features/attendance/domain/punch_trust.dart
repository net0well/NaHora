/// How much a recorded punch can be relied on.
///
/// Never a reason the punch does not exist. Portaria 671, Art. 78 §1º forbids
/// the registrar from restricting the record, so a mocked position or a device
/// clock hours out of step changes this value and nothing else.
///
/// The label matters as much as the colour: roughly 8% of men have some colour
/// vision deficiency, and a row distinguished only by an amber tint would tell
/// them nothing.
enum PunchTrust {
  trusted('Trusted', 'Registrada'),
  degraded('Degraded', 'Precisão baixa'),
  needsReview('NeedsReview', 'Em análise');

  const PunchTrust(this.code, this.label);

  /// What the API sends. The contract, not the display text.
  final String code;

  /// What the employee reads.
  final String label;

  /// Falls back to [trusted] for a value this build does not know.
  ///
  /// A newer server adding a classification must not make an employee's own
  /// record unreadable to them — the punch is there either way.
  static PunchTrust fromCode(String code) => values.firstWhere(
    (trust) => trust.code == code,
    orElse: () => PunchTrust.trusted,
  );
}
