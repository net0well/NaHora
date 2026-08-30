import 'package:flutter/material.dart';

import '../theme/app_colors.dart';
import '../theme/app_spacing.dart';
import '../theme/app_typography.dart';

/// A label-and-value list, for the records a screen exists to display.
///
/// Rows whose value is null are dropped rather than shown empty. A field a
/// company legitimately does not have — a state registration when it is exempt
/// — reads as missing data if rendered with a dash, and somebody eventually
/// tries to "fix" it.
class AppDetailList extends StatelessWidget {
  const AppDetailList({required this.entries, super.key});

  final List<AppDetail> entries;

  @override
  Widget build(BuildContext context) {
    final present = entries.where((entry) => entry.value != null).toList();

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        for (var index = 0; index < present.length; index++) ...[
          if (index > 0) const SizedBox(height: AppSpacing.md),
          _Row(detail: present[index]),
        ],
      ],
    );
  }
}

/// One label and its value. A null value means the row is not rendered.
class AppDetail {
  const AppDetail(this.label, this.value);

  final String label;
  final String? value;
}

class _Row extends StatelessWidget {
  const _Row({required this.detail});

  final AppDetail detail;

  @override
  Widget build(BuildContext context) {
    // One semantics node per row: a screen reader should announce "CNPJ,
    // 11.222.333/0001-81", not two disconnected fragments.
    return Semantics(
      label: '${detail.label}: ${detail.value}',
      child: ExcludeSemantics(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              detail.label,
              style: AppTypography.labelSmall.copyWith(
                color: AppColors.textSecondary,
              ),
            ),
            const SizedBox(height: 2),
            Text(
              detail.value!,
              style: AppTypography.bodyMedium.copyWith(
                color: AppColors.textPrimary,
              ),
            ),
          ],
        ),
      ),
    );
  }
}
