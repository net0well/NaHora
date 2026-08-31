/// What a correction asks for.
///
/// Changing a time and adding one that was never recorded are different
/// requests: the first has an original to show beside the new value, the second
/// has nothing to compare against.
enum AdjustmentKind {
  timeChange('TimeChange', 'Correção de horário'),
  inclusion('Inclusion', 'Inclusão de marcação');

  const AdjustmentKind(this.code, this.label);

  /// What the API sends.
  final String code;

  /// What the employee and the manager read.
  final String label;

  static AdjustmentKind fromCode(String code) => values.firstWhere(
    (kind) => kind.code == code,
    orElse: () => AdjustmentKind.timeChange,
  );
}
