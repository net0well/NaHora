/// A reason a correction may be asked for.
///
/// The catalogue is fixed and comes from the server: a free-text reason would
/// make two people describe the same situation differently, and an audit that
/// has to read prose is an audit nobody does.
class Justification {
  const Justification({
    required this.id,
    required this.code,
    required this.description,
    required this.requiresNotes,
    required this.displayOrder,
  });

  factory Justification.fromJson(Map<String, dynamic> json) => Justification(
    id: json['id'] as String,
    code: json['code'] as String,
    description: json['description'] as String,
    requiresNotes: (json['requiresNotes'] as bool?) ?? false,
    displayOrder: (json['displayOrder'] as int?) ?? 0,
  );

  final String id;

  /// The stable identifier. The description can be rewritten; this cannot.
  final String code;

  final String description;

  /// True for the catch-all. Without text "Outro" becomes the way to avoid
  /// choosing, so the form refuses to submit until something is written.
  final bool requiresNotes;

  final int displayOrder;
}
