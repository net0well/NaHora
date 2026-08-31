import 'package:flutter/material.dart';

import '../theme/app_colors.dart';
import '../theme/app_radius.dart';
import '../theme/app_spacing.dart';
import '../theme/app_typography.dart';

/// One working day in a list, opening to show what was recorded in it.
///
/// A history is mostly days, not punches. Printing every record flat turns a
/// month into a wall somebody has to scroll past to find the one Tuesday they
/// came for; one row per day makes the month readable and the day one tap away.
class AppExpandableDay extends StatefulWidget {
  const AppExpandableDay({
    required this.title,
    required this.summary,
    required this.children,
    super.key,
    this.trailing,
    this.initiallyExpanded = false,
  });

  /// The date, already formatted — "30/08/2026".
  final String title;

  /// What happened that day, in a few words: "4 marcações · 08:02 às 17:05".
  final String summary;

  /// The rows revealed when it opens.
  final List<Widget> children;

  /// Actions that belong to the day rather than to one punch.
  final Widget? trailing;

  final bool initiallyExpanded;

  @override
  State<AppExpandableDay> createState() => _AppExpandableDayState();
}

class _AppExpandableDayState extends State<AppExpandableDay> {
  late bool _isExpanded = widget.initiallyExpanded;

  void _toggle() => setState(() => _isExpanded = !_isExpanded);

  @override
  Widget build(BuildContext context) {
    // Material rather than a decorated box: the header ripples, and an InkWell
    // with no Material above it has nothing to ripple on.
    return Material(
      color: AppColors.surface,
      clipBehavior: Clip.antiAlias,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(AppRadius.md),
        side: const BorderSide(color: AppColors.border),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Semantics(
            button: true,
            expanded: _isExpanded,
            label: '${widget.title}. ${widget.summary}',
            child: ExcludeSemantics(
              child: InkWell(
                onTap: _toggle,
                borderRadius: BorderRadius.circular(AppRadius.md),
                child: Padding(
                  padding: const EdgeInsets.all(AppSpacing.md),
                  child: Row(
                    children: [
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              widget.title,
                              style: AppTypography.bodyLarge.copyWith(
                                color: AppColors.textPrimary,
                                fontWeight: FontWeight.w600,
                              ),
                            ),
                            const SizedBox(height: 2),
                            Text(
                              widget.summary,
                              style: AppTypography.labelSmall.copyWith(
                                color: AppColors.textSecondary,
                              ),
                            ),
                          ],
                        ),
                      ),

                      // Turns rather than swaps, so the row does not jump and
                      // the direction of travel stays legible.
                      AnimatedRotation(
                        turns: _isExpanded ? 0.5 : 0,
                        duration: const Duration(milliseconds: 160),
                        child: const Icon(
                          Icons.expand_more,
                          color: AppColors.textSecondary,
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ),
          ),
          if (_isExpanded) ...[
            const Divider(height: 1, color: AppColors.border),
            Padding(
              padding: const EdgeInsets.fromLTRB(
                AppSpacing.md,
                AppSpacing.sm,
                AppSpacing.md,
                AppSpacing.md,
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  ...widget.children,
                  if (widget.trailing != null) ...[
                    const SizedBox(height: AppSpacing.md),
                    widget.trailing!,
                  ],
                ],
              ),
            ),
          ],
        ],
      ),
    );
  }
}
