/// What the request form is doing.
class RequestAdjustmentState {
  const RequestAdjustmentState({
    this.isSubmitting = false,
    this.errorMessage,
    this.isSent = false,
    this.attachmentFailed = false,
  });

  final bool isSubmitting;

  /// What went wrong, already translated. Null when nothing did.
  final String? errorMessage;

  /// The request reached the server and is now waiting on a manager.
  final bool isSent;

  /// The request went through but its document did not.
  ///
  /// Tracked apart from [errorMessage] because it is not a failure of the
  /// request: the correction exists and a manager will see it. Telling somebody
  /// their pedido failed when it did not would have them send it twice.
  final bool attachmentFailed;
}
