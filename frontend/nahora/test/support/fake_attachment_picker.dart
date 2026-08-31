import 'package:nahora/features/adjustments/domain/attachment_picker.dart';
import 'package:nahora/features/adjustments/domain/attachment_source.dart';

/// A camera and a gallery a test decides for.
///
/// The picker opens another application, which no test can drive — and backing
/// out of it is the case worth covering.
class FakeAttachmentPicker implements AttachmentPicker {
  /// What the picker returns. Null means the person backed out.
  String? path = '/storage/emulated/0/DCIM/atestado.jpg';

  /// Every source asked for, in order.
  final List<AttachmentSource> asked = [];

  @override
  Future<String?> pick(AttachmentSource source) async {
    asked.add(source);

    return path;
  }
}
