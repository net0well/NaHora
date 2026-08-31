import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/config/app_config.dart';
import '../../../design_system/components/app_empty_state.dart';
import '../../../design_system/components/app_loading.dart';
import '../../../design_system/components/app_map.dart';
import '../../../design_system/theme/app_colors.dart';
import '../../../design_system/theme/app_radius.dart';
import '../../../design_system/theme/app_spacing.dart';
import '../../../design_system/theme/app_typography.dart';
import '../domain/punch_entry.dart';
import 'attendance_providers.dart';
import 'punch_entry_formatting.dart';

/// Where the marks of one day were made.
///
/// One map, one mark at a time, chosen by its hour. The coordinates arrive only
/// when somebody asks for them: no listing carries a position, so opening this
/// sheet is the only thing that puts one on the device.
class DayLocationsSheet extends ConsumerStatefulWidget {
  const DayLocationsSheet({
    required this.dayLabel,
    required this.entries,
    super.key,
  });

  final String dayLabel;

  /// The day's marks, in the order the screen shows them.
  final List<PunchEntry> entries;

  @override
  ConsumerState<DayLocationsSheet> createState() => _DayLocationsSheetState();
}

class _DayLocationsSheetState extends ConsumerState<DayLocationsSheet> {
  /// Marks that can have a position. An inclusion was never captured, so there
  /// is nothing to ask the server for.
  late final List<PunchEntry> _locatable = widget.entries
      .where((entry) => !entry.isIncluded)
      .toList();

  late String? _selectedId = _locatable.isEmpty ? null : _locatable.first.id;

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.lg),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Semantics(
              header: true,
              child: Text(
                'Localização · ${widget.dayLabel}',
                style: AppTypography.titleMedium.copyWith(
                  color: AppColors.textPrimary,
                ),
              ),
            ),
            const SizedBox(height: AppSpacing.md),
            if (_selectedId == null)
              const AppEmptyState(
                key: Key('day_locations_empty'),
                icon: Icons.location_off_outlined,
                title: 'Sem localização neste dia',
                message:
                    'As marcações deste dia foram incluídas por ajuste, e um '
                    'ajuste não tem posição para mostrar.',
              )
            else ...[
              _Hours(
                entries: _locatable,
                selectedId: _selectedId!,
                onSelected: (id) => setState(() => _selectedId = id),
              ),
              const SizedBox(height: AppSpacing.md),
              _Map(punchId: _selectedId!),
            ],
          ],
        ),
      ),
    );
  }
}

/// Which mark of the day to look at.
class _Hours extends StatelessWidget {
  const _Hours({
    required this.entries,
    required this.selectedId,
    required this.onSelected,
  });

  final List<PunchEntry> entries;
  final String selectedId;
  final ValueChanged<String> onSelected;

  @override
  Widget build(BuildContext context) {
    return Wrap(
      spacing: AppSpacing.sm,
      runSpacing: AppSpacing.sm,
      children: [
        for (final entry in entries)
          _Hour(
            key: Key('day_location_hour_${entry.id}'),
            label: entry.timeLabel,
            description: entry.direction.label,
            isSelected: entry.id == selectedId,
            onTap: () => onSelected(entry.id),
          ),
      ],
    );
  }
}

class _Hour extends StatelessWidget {
  const _Hour({
    required this.label,
    required this.description,
    required this.isSelected,
    required this.onTap,
    super.key,
  });

  final String label;
  final String description;
  final bool isSelected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Semantics(
      button: true,
      selected: isSelected,
      label: '$label, $description',
      onTap: onTap,
      child: ExcludeSemantics(
        child: Material(
          color: isSelected ? AppColors.primary : AppColors.surfaceMuted,
          clipBehavior: Clip.antiAlias,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(AppRadius.sm),
            side: const BorderSide(color: AppColors.border),
          ),
          child: InkWell(
            onTap: onTap,
            child: Padding(
              padding: const EdgeInsets.symmetric(
                horizontal: AppSpacing.md,
                vertical: AppSpacing.sm,
              ),
              child: Text(
                label,
                style: AppTypography.labelLarge.copyWith(
                  color: isSelected
                      ? AppColors.onPrimary
                      : AppColors.textSecondary,
                  fontWeight: FontWeight.w600,
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class _Map extends ConsumerWidget {
  const _Map({required this.punchId});

  final String punchId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final location = ref.watch(punchLocationProvider(punchId));

    return location.when(
      loading: () => const SizedBox(
        height: 260,
        child: AppLoading(message: 'Buscando a localização'),
      ),

      // Falls back to the same card the punch screen shows without a fix: a
      // position that cannot be fetched is not an error worth a red banner,
      // and the mark itself is unaffected either way.
      error: (error, _) => const AppMap(
        key: Key('day_location_map'),
        height: 260,
        tileKey: '',
        isUnavailable: true,
      ),
      data: (value) => AppMap(
        key: const Key('day_location_map'),
        height: 260,
        tileKey: AppConfig.mapTilerKey,
        latitude: value.latitude,
        longitude: value.longitude,
        accuracyMeters: value.accuracyMeters,
        isInteractive: true,
      ),
    );
  }
}
