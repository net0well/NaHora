/// The login form as the screen renders it.
class LoginState {
  const LoginState({this.isSubmitting = false, this.errorMessage});

  final bool isSubmitting;

  /// Already translated for a person. The controller resolves the backend code
  /// so the widget has nothing to decide.
  final String? errorMessage;

  LoginState copyWith({bool? isSubmitting, String? errorMessage}) => LoginState(
    isSubmitting: isSubmitting ?? this.isSubmitting,
    errorMessage: errorMessage,
  );
}
