import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../design_system/components/app_empty_state.dart';
import '../../../design_system/components/app_error_state.dart';
import '../../../design_system/components/app_loading.dart';
import '../../../design_system/components/punch_entry_tile.dart';
import '../../../design_system/theme/app_colors.dart';
import '../../../design_system/theme/app_spacing.dart';
import '../../../design_system/theme/app_typography.dart';
import '../../attendance/domain/punch_day.dart';
import '../../attendance/presentation/attendance_providers.dart';
import '../../attendance/presentation/punch_entry_formatting.dart';
import '../../auth/presentation/auth_providers.dart';

/// Who the employee is, and what they have recorded today.
class EmployeeTab extends ConsumerWidget {
  const EmployeeTab({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final session = ref.watch(currentSessionProvider);
    final day = ref.watch(punchDayProvider);

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
            const SizedBox(height: AppSpacing.lg),
            Expanded(
              child: day.when(
                loading: () =>
                    const AppLoading(message: 'Carregando suas marcações'),
                error: (error, _) => AppErrorState(
                  message: 'Não foi possível carregar suas marcações de hoje.',
                  onRetry: () => ref.invalidate(punchDayProvider),
                ),
                data: (value) => _Today(day: value),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

/// Today's punches, or an honest blank.
class _Today extends ConsumerWidget {
  const _Today({required this.day});

  final PunchDay day;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    if (day.isEmpty) {
      return const AppEmptyState(
        key: Key('employee_tab_empty'),
        icon: Icons.schedule_outlined,
        title: 'Nenhuma marcação ainda',
        message:
            'Suas marcações do dia aparecem aqui assim que você bater o '
            'primeiro ponto.',
      );
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text(
          // Named so the employee knows what the button will record before
          // pressing it, not after.
          'Próxima: ${day.nextDirection.label}',
          style: AppTypography.labelSmall.copyWith(color: AppColors.primary),
        ),
        const SizedBox(height: AppSpacing.sm),
        Expanded(
          child: ListView.separated(
            key: const Key('employee_tab_list'),
            padding: EdgeInsets.zero,
            itemCount: day.entries.length,
            separatorBuilder: (_, _) => const SizedBox(height: AppSpacing.sm),
            itemBuilder: (context, index) {
              final entry = day.entries[index];

              return PunchEntryTile(
                time: entry.timeLabel,
                direction: entry.note == null
                    ? entry.direction.label
                    : '${entry.direction.label} · ${entry.note}',
                tone: entry.tone,
              );
            },
          ),
        ),
      ],
    );
  }
}
