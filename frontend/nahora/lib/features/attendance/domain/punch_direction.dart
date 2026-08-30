/// Which boundary of the working day a punch marks.
///
/// The server decides which comes next; the app only renders it. Letting the
/// employee choose would allow two entries in a row, and the correction lands
/// on a manager.
enum PunchDirection {
  entry('Entry', 'Entrada'),
  breakStart('BreakStart', 'Início do intervalo'),
  breakEnd('BreakEnd', 'Retorno do intervalo'),
  exit('Exit', 'Saída');

  const PunchDirection(this.code, this.label);

  /// What the API sends. The contract, not the display text.
  final String code;

  /// What the employee reads.
  final String label;

  static PunchDirection fromCode(String code) => values.firstWhere(
    (direction) => direction.code == code,
    orElse: () => PunchDirection.entry,
  );
}
