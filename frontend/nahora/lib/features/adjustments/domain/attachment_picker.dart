import 'attachment_source.dart';

/// Picks the file that backs a correction.
///
/// A port rather than a direct call to the plugin: the picker opens another
/// application, which no test can drive, and the form's behaviour around a
/// cancelled pick is exactly what needs covering.
abstract interface class AttachmentPicker {
  /// The chosen file's path, or null when the person backed out.
  Future<String?> pick(AttachmentSource source);
}
