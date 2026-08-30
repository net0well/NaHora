import 'package:flutter/material.dart';

import '../../../design_system/components/app_empty_state.dart';
import '../../../design_system/theme/app_colors.dart';
import '../../../design_system/theme/app_spacing.dart';
import '../../../design_system/theme/app_typography.dart';

/// Past days, once the history endpoint exists.
class HistoryTab extends StatelessWidget {
  const HistoryTab({super.key});

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.lg),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Semantics(
              header: true,
              child: Text(
                'Histórico',
                style: AppTypography.headlineMedium.copyWith(
                  color: AppColors.textPrimary,
                ),
              ),
            ),
            const SizedBox(height: AppSpacing.xl),
            const Expanded(
              child: AppEmptyState(
                icon: Icons.calendar_month_outlined,
                title: 'Histórico em construção',
                message:
                    'Assim que o servidor passar a devolver os dias anteriores, '
                    'eles aparecem aqui com horário, origem e localização.',
              ),
            ),
          ],
        ),
      ),
    );
  }
}
