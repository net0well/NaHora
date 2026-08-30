import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/errors/app_exception.dart';
import 'auth_providers.dart';
import 'login_state.dart';

/// Drives the login form.
///
/// Separate from [AuthController] because they answer different questions: this
/// one owns a form that is being filled in, that one owns the session for the
/// whole app. Merging them would put a submit button's spinner in the state
/// every screen watches.
class LoginController extends AutoDisposeNotifier<LoginState> {
  @override
  LoginState build() => const LoginState();

  Future<bool> submit({
    required String cpf,
    required String password,
    required String companyId,
  }) async {
    if (state.isSubmitting) {
      return false;
    }

    state = const LoginState(isSubmitting: true);

    try {
      await ref
          .read(authControllerProvider.notifier)
          .signIn(cpf: cpf, password: password, companyId: companyId);

      // Reset even though the router is about to navigate away. Leaving the
      // flag set means a spinner that never stops if navigation does not
      // happen, and that is exactly the case nobody notices in development.
      state = const LoginState();

      return true;
    } on AppException catch (exception) {
      // The code is translated here so the widget receives text it can render
      // and nothing it has to interpret. The raw code goes nowhere near the
      // screen.
      state = LoginState(errorMessage: ErrorMessages.resolve(exception.code));

      return false;
    }
  }
}

final loginControllerProvider =
    AutoDisposeNotifierProvider<LoginController, LoginState>(
      LoginController.new,
    );
