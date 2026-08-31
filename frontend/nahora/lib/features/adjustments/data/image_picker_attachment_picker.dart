import 'package:image_picker/image_picker.dart';

import '../domain/attachment_picker.dart';
import '../domain/attachment_source.dart';

/// The device's camera and gallery.
class ImagePickerAttachmentPicker implements AttachmentPicker {
  ImagePickerAttachmentPicker({ImagePicker? picker})
    : _picker = picker ?? ImagePicker();

  final ImagePicker _picker;

  /// Downscaled before it leaves the device. A modern phone camera produces
  /// files several megabytes larger than anything a manager needs to read a
  /// receipt, and the upload happens on whatever signal the person has.
  static const int _maxEdge = 1600;
  static const int _quality = 80;

  @override
  Future<String?> pick(AttachmentSource source) async {
    final file = await _picker.pickImage(
      source: source == AttachmentSource.camera
          ? ImageSource.camera
          : ImageSource.gallery,
      maxWidth: _maxEdge.toDouble(),
      maxHeight: _maxEdge.toDouble(),
      imageQuality: _quality,
    );

    return file?.path;
  }
}
