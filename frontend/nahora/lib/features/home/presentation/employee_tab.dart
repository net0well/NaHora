import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../design_system/components/app_empty_state.dart';
import '../../../design_system/theme/app_colors.dart';
import '../../../design_system/theme/app_spacing.dart';
import '../../../design_system/theme/app_typography.dart';
import '../../auth/presentation/auth_providers.dart';

/// Who the employee is, and what they have recorded today.
class EmployeeTab extends ConsumerWidget {
  const EmployeeTab({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final session = ref.watch(currentSessionProvider);

    return SafeArea(
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.lg),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              'Olá,',
              style: AppTypography.bodyMedium.copyWith(
                color: AppColors.textSecondary,
              ),
            ),
            Semantics(
              header: true,
              child: Text(
                session?.user.fullName ?? 'Bem-vindo',
                style: AppTypography.headlineMedium.copyWith(
                  color: AppColors.textPrimary,
                ),
              ),
            ),
            const SizedBox(height: AppSpacing.xl),

            // Honest about being unbuilt rather than showing invented rows.
            // Sample data on a compliance screen is worse than an empty one:
            // somebody eventually reads it as real.
            const Expanded(
              child: AppEmptyState(
                icon: Icons.schedule_outlined,
                title: 'Nenhuma marcação ainda',
                message:
                    'Suas marcações do dia aparecem aqui assim que você bater '
                    'o primeiro ponto.',
              ),
            ),
          ],
        ),
      ),
    );
  }
}
