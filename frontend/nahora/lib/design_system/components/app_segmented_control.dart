import 'package:flutter/material.dart';

import '../theme/app_colors.dart';
import '../theme/app_radius.dart';
import '../theme/app_spacing.dart';
import '../theme/app_typography.dart';

/// A choice between two or three views of the same screen.
///
/// Both options stay on screen, which is what separates this from a menu: the
/// person can see that the other view exists without opening anything, and
/// switching costs one tap in either direction.
class AppSegmentedControl extends StatelessWidget {
  const AppSegmentedControl({
    required this.segments,
    required this.selectedId,
    required this.onChanged,
    super.key,
  });

  final List<AppSegment> segments;

  final String selectedId;

  final ValueChanged<String> onChanged;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: AppColors.surfaceMuted,
      clipBehavior: Clip.antiAlias,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(AppRadius.md),
        side: const BorderSide(color: AppColors.border),
      ),
      child: Row(
        children: [
          for (final segment in segments)
            Expanded(
              child: _Segment(
                segment: segment,
                isSelected: segment.id == selectedId,
                onTap: () => onChanged(segment.id),
              ),
            ),
        ],
      ),
    );
  }
}

class _Segment extends StatelessWidget {
  const _Segment({
    required this.segment,
    required this.isSelected,
    required this.onTap,
  });

  final AppSegment segment;
  final bool isSelected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final foreground = isSelected
        ? AppColors.onPrimary
        : AppColors.textSecondary;

    return Semantics(
      button: true,
      selected: isSelected,
      label: segment.label,

      // Carried on the node itself, because the InkWell below is excluded from
      // the tree: without it assistive technology can read the segment and not
      // switch to it.
      onTap: onTap,
      child: ExcludeSemantics(
        child: InkWell(
          key: Key('segment_${segment.id}'),
          onTap: onTap,
          child: Ink(
            color: isSelected ? AppColors.primary : Colors.transparent,
            child: Padding(
              padding: const EdgeInsets.symmetric(
                vertical: AppSpacing.md,
                horizontal: AppSpacing.sm,
              ),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Flexible(
                    child: Text(
                      segment.label,
                      overflow: TextOverflow.ellipsis,
                      style: AppTypography.labelLarge.copyWith(
                        color: foreground,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                  ),

                  // The count rides inside the segment rather than beside the
                  // control: it belongs to one of the two views, and a badge
                  // floating between them would not say which.
                  if (segment.badge != null) ...[
                    const SizedBox(width: AppSpacing.sm),
                    _Count(value: segment.badge!, isSelected: isSelected),
                  ],
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class _Count extends StatelessWidget {
  const _Count({required this.value, required this.isSelected});

  final String value;
  final bool isSelected;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 1),
      decoration: BoxDecoration(
        color: isSelected ? AppColors.onPrimary : AppColors.primary,
        borderRadius: BorderRadius.circular(AppRadius.sm),
      ),
      child: Text(
        value,
        style: AppTypography.labelSmall.copyWith(
          color: isSelected ? AppColors.primary : AppColors.onPrimary,
          fontWeight: FontWeight.w700,
        ),
      ),
    );
  }
}

/// One option in an [AppSegmentedControl].
class AppSegment {
  const AppSegment({required this.id, required this.label, this.badge});

  /// Stable identifier. Also the widget key, so a test finds the segment by
  /// what it is rather than by what it currently says.
  final String id;

  final String label;

  /// A short count, when the view behind this segment is waiting on something.
  final String? badge;
}
