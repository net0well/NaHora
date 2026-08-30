import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../design_system/components/app_button.dart';
import '../../../design_system/components/app_button_variant.dart';
import '../../../design_system/components/app_empty_state.dart';
import '../../../design_system/components/app_error_state.dart';
import '../../../design_system/components/app_expandable_day.dart';
import '../../../design_system/components/app_loading.dart';
import '../../../design_system/components/punch_entry_tile.dart';
import '../../../design_system/theme/app_colors.dart';
import '../../../design_system/theme/app_spacing.dart';
import '../../../design_system/theme/app_typography.dart';
import '../../attendance/domain/punch_entry.dart';
import '../../attendance/domain/punch_history_page.dart';
import '../../attendance/presentation/attendance_providers.dart';
import '../../attendance/presentation/punch_entry_formatting.dart';

/// Every punch the employee has, one row per day.
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

    return ListView.separated(
      key: const Key('history_tab_list'),
      padding: EdgeInsets.zero,
      itemCount: days.length,
      separatorBuilder: (_, _) => const SizedBox(height: AppSpacing.sm),
      itemBuilder: (context, index) {
        final day = days[index];

        return AppExpandableDay(
          key: Key('history_day_${day.key}'),
          title: _dayLabel(day.key),
          summary: _summarise(day.value),

          // The most recent day starts open. It is the one somebody came for
          // in almost every visit, and making them tap for it is a tap for
          // nothing.
          initiallyExpanded: index == 0,
          trailing: _DayActions(day: day.key, entries: day.value),
          children: [
            for (final entry in day.value) ...[
              PunchEntryTile(
                key: Key('history_entry_${entry.nsr}'),
                time: entry.timeLabel,
                direction: entry.note == null
                    ? entry.direction.label
                    : '${entry.direction.label} · ${entry.note}',
                tone: entry.tone,
              ),
              const SizedBox(height: AppSpacing.sm),
            ],
          ],
        );
      },
    );
  }

  /// "4 marcações · 08:02 às 17:05"
  static String _summarise(List<PunchEntry> entries) {
    final count = entries.length;
    final noun = count == 1 ? 'marcação' : 'marcações';

    // Ordered by time rather than by arrival: the list comes newest first, and
    // a summary reading "17:05 às 08:02" would look like a mistake.
    final times = entries.map((entry) => entry.timeLabel).toList()..sort();

    return times.first == times.last
        ? '$count $noun · ${times.first}'
        : '$count $noun · ${times.first} às ${times.last}';
  }

  /// Groups by the working day the server decided, preserving its order.
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

/// What can be done about a day, rather than about one punch.
class _DayActions extends StatelessWidget {
  const _DayActions({required this.day, required this.entries});

  final String day;
  final List<PunchEntry> entries;

  @override
  Widget build(BuildContext context) {
    // Stacked, not side by side: the two labels together are wider than a card
    // on a phone leaves, and they are distinct actions rather than a pair.
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        AppButton(
          key: Key('history_map_$day'),
          label: 'Ver localização',
          variant: AppButtonVariant.secondary,
          expanded: true,

          // Disabled rather than hidden while the map is unbuilt: a control
          // that appears later moves everything under it, and somebody who
          // learned where it was has to learn again.
          onPressed: null,
        ),
        const SizedBox(height: AppSpacing.sm),
        AppButton(
          key: Key('history_adjust_$day'),
          label: 'Solicitar ajuste',
          variant: AppButtonVariant.secondary,
          expanded: true,
          onPressed: null,
        ),
      ],
    );
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
