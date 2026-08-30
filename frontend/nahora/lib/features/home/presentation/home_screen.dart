import 'package:flutter/material.dart';

import '../../../design_system/components/app_button.dart';
import '../../../design_system/components/app_button_variant.dart';
import '../../../design_system/theme/app_colors.dart';
import '../../../design_system/theme/app_spacing.dart';
import '../../../design_system/theme/app_typography.dart';

/// Landing screen for the foundation build. Replaced by the login flow once
/// authentication lands.
class HomeScreen extends StatelessWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Center(
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 460),
          child: Padding(
            padding: const EdgeInsets.all(AppSpacing.xl),
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                const Icon(
                  Icons.access_time_filled_outlined,
                  size: 72,
                  color: AppColors.primary,
                ),
                const SizedBox(height: AppSpacing.lg),
                Text(
                  'NaHora',
                  key: const Key('home_title'),
                  style: AppTypography.displayLarge,
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: AppSpacing.sm),
                Text(
                  'Controle de ponto com reconhecimento facial',
                  key: const Key('home_subtitle'),
                  style: AppTypography.bodyMedium,
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: AppSpacing.xxl),
                AppButton(
                  key: const Key('home_login_button'),
                  label: 'Entrar',
                  icon: Icons.login,
                  expanded: true,
                  onPressed: () {},
                ),
                const SizedBox(height: AppSpacing.sm),
                AppButton(
                  key: const Key('home_register_company_button'),
                  label: 'Cadastrar empresa',
                  variant: AppButtonVariant.secondary,
                  expanded: true,
                  onPressed: () {},
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
