/// Where a correction stands.
///
/// The codes are the server's contract; the labels are what the two people
/// involved read. A manager sees a queue of the first one and nothing else.
enum AdjustmentStatus {
  pending('Pending', 'Aguardando aprovação'),
  approved('Approved', 'Aprovado'),
  rejected('Rejected', 'Recusado');

  const AdjustmentStatus(this.code, this.label);

  /// What the API sends.
  final String code;

  /// What the employee and the manager read.
  final String label;

  static AdjustmentStatus fromCode(String code) => values.firstWhere(
    (status) => status.code == code,
    orElse: () => AdjustmentStatus.pending,
  );
}
