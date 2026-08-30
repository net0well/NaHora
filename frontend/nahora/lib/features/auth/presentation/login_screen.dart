import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/utils/cpf_input_formatter.dart';
import '../../../design_system/components/app_button.dart';
import '../../../design_system/components/app_text_field.dart';
import '../../../design_system/theme/app_colors.dart';
import '../../../design_system/theme/app_radius.dart';
import '../../../design_system/theme/app_spacing.dart';
import '../../../design_system/theme/app_typography.dart';
import 'login_controller.dart';

/// Sign-in by CPF and password.
class LoginScreen extends ConsumerStatefulWidget {
  const LoginScreen({super.key, this.companyId = ''});

  /// Which company the employee belongs to.
  ///
  /// Comes from the invitation link rather than from a field: asking someone to
  /// type a company identifier to clock in would be asking for the one thing
  /// they have no way of knowing.
  final String companyId;

  @override
  ConsumerState<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends ConsumerState<LoginScreen> {
  final _cpf = TextEditingController();
  final _password = TextEditingController();
  final _passwordFocus = FocusNode();

  @override
  void dispose() {
    _cpf.dispose();
    _password.dispose();
    _passwordFocus.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    await ref
        .read(loginControllerProvider.notifier)
        .submit(
          cpf: CpfInputFormatter.digitsOf(_cpf.text),
          password: _password.text,
          companyId: widget.companyId,
        );
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(loginControllerProvider);

    return Scaffold(
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(AppSpacing.lg),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 420),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  // The mark carries the name, so there is no separate title.
                  // Printing "NaHora" underneath would say it twice.
                  //
                  // Rounded because the artwork has its own opaque, darker
                  // ground: left square it reads as a mismatched block over the
                  // page, and as a deliberate tile once the corners are cut.
                  Semantics(
                    header: true,
                    label: 'NaHora',
                    child: ExcludeSemantics(
                      child: Center(
                        child: ClipRRect(
                          borderRadius: BorderRadius.circular(AppRadius.lg),
                          child: Image.asset(
                            'assets/images/nahora-logo.png',
                            key: const Key('login_logo'),
                            height: 180,
                            fit: BoxFit.contain,
                          ),
                        ),
                      ),
                    ),
                  ),
                  const SizedBox(height: AppSpacing.lg),
                  Text(
                    'Entre para marcar seu ponto',
                    style: AppTypography.bodyMedium.copyWith(
                      color: AppColors.textSecondary,
                    ),
                  ),
                  const SizedBox(height: AppSpacing.xl),

                  AppTextField(
                    key: const Key('login_cpf'),
                    label: 'CPF',
                    placeholder: '000.000.000-00',
                    controller: _cpf,
                    keyboardType: TextInputType.number,
                    inputFormatters: [
                      FilteringTextInputFormatter.digitsOnly,
                      CpfInputFormatter(),
                    ],
                    onSubmitted: (_) => _passwordFocus.requestFocus(),
                  ),
                  const SizedBox(height: AppSpacing.md),

                  AppTextField(
                    key: const Key('login_password'),
                    label: 'Senha',
                    controller: _password,
                    focusNode: _passwordFocus,
                    obscureText: true,
                    onSubmitted: (_) => _submit(),
                  ),

                  if (state.errorMessage != null) ...[
                    const SizedBox(height: AppSpacing.md),
                    _ErrorBanner(message: state.errorMessage!),
                  ],

                  const SizedBox(height: AppSpacing.lg),
                  AppButton(
                    key: const Key('login_submit'),
                    label: 'Entrar',
                    isLoading: state.isSubmitting,
                    expanded: true,
                    onPressed: _submit,
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}

/// The failure message, announced rather than merely drawn.
///
/// A screen reader user who taps Entrar and hears nothing has no way to know
/// the attempt failed.
class _ErrorBanner extends StatelessWidget {
  const _ErrorBanner({required this.message});

  final String message;

  @override
  Widget build(BuildContext context) {
    return Semantics(
      liveRegion: true,
      label: message,
      child: ExcludeSemantics(
        child: Container(
          key: const Key('login_error'),
          padding: const EdgeInsets.all(AppSpacing.md),
          decoration: BoxDecoration(
            color: AppColors.dangerLight,
            borderRadius: BorderRadius.circular(AppRadius.sm),
          ),
          child: Row(
            children: [
              const Icon(
                Icons.error_outline,
                size: 18,
                color: AppColors.danger,
              ),
              const SizedBox(width: AppSpacing.sm),
              Expanded(
                child: Text(
                  message,
                  style: AppTypography.bodyMedium.copyWith(
                    color: AppColors.danger,
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
