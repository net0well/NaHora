/// Where the document backing a correction comes from.
enum AttachmentSource {
  /// Taken now. The usual case for a receipt or a note from a doctor.
  camera,

  /// Chosen from what is already on the device.
  gallery,
}
