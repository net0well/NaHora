import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../design_system/components/app_empty_state.dart';
import '../../../design_system/components/app_error_state.dart';
import '../../../design_system/components/app_loading.dart';
import '../../../design_system/components/punch_entry_tile.dart';
import '../../../design_system/theme/app_colors.dart';
import '../../../design_system/theme/app_spacing.dart';
import '../../../design_system/theme/app_typography.dart';
import '../../attendance/domain/punch_entry.dart';
import '../../attendance/domain/punch_history_page.dart';
import '../../attendance/presentation/attendance_providers.dart';
import '../../attendance/presentation/punch_entry_formatting.dart';

/// Every punch the employee has, newest first.
///
/// Portaria 671 gives them the right to their own record. This is where that
/// right stops being a sentence in a spec.
class HistoryTab extends ConsumerWidget {
  const HistoryTab({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final history = ref.watch(punchHistoryProvider);

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
            const SizedBox(height: AppSpacing.lg),
            Expanded(
              child: history.when(
                loading: () =>
                    const AppLoading(message: 'Carregando seu histórico'),
                error: (error, _) => AppErrorState(
                  message: 'Não foi possível carregar seu histórico.',
                  onRetry: () => ref.invalidate(punchHistoryProvider),
                ),
                data: (value) => _History(page: value),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _History extends ConsumerWidget {
  const _History({required this.page});

  final PunchHistoryPage page;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    if (page.entries.isEmpty) {
      return const AppEmptyState(
        key: Key('history_tab_empty'),
        icon: Icons.history,
        title: 'Nada registrado ainda',
        message:
            'Assim que você bater o primeiro ponto, ele aparece aqui com a '
            'data e o número do registro.',
      );
    }

    final days = _groupByDay(page.entries);

    return ListView.builder(
      key: const Key('history_tab_list'),
      padding: EdgeInsets.zero,
      itemCount: days.length,
      itemBuilder: (context, index) {
        final day = days[index];

        return Padding(
          padding: const EdgeInsets.only(bottom: AppSpacing.lg),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text(
                _dayLabel(day.key),
                style: AppTypography.labelSmall.copyWith(
                  color: AppColors.textSecondary,
                ),
              ),
              const SizedBox(height: AppSpacing.sm),
              for (final entry in day.value) ...[
                PunchEntryTile(
                  time: entry.timeLabel,
                  direction: entry.note == null
                      ? entry.direction.label
                      : '${entry.direction.label} · ${entry.note}',
                  tone: entry.tone,
                ),
                const SizedBox(height: AppSpacing.sm),
              ],
            ],
          ),
        );
      },
    );
  }

  /// Groups by the working day the server decided, preserving the order the
  /// server sent.
  ///
  /// Grouping on a date recomputed from the timestamp would split one working
  /// day in two for anyone whose phone sits in another timezone.
  static List<MapEntry<String, List<PunchEntry>>> _groupByDay(
    List<PunchEntry> entries,
  ) {
    final days = <String, List<PunchEntry>>{};

    for (final entry in entries) {
      days.putIfAbsent(entry.localDate ?? '', () => []).add(entry);
    }

    return days.entries.toList();
  }
}

/// "30/08/2026" from the server's "2026-08-30".
String _dayLabel(String isoDate) {
  final parts = isoDate.split('-');
  if (parts.length != 3) {
    return isoDate;
  }

  return '${parts[2]}/${parts[1]}/${parts[0]}';
}
