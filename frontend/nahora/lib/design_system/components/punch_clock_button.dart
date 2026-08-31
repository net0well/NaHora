import 'package:flutter/material.dart';

import '../theme/app_colors.dart';
import '../theme/app_radius.dart';
import '../theme/app_spacing.dart';
import '../theme/app_typography.dart';

/// The clock and the action, in one control.
///
/// The employee looks at the time and their thumb is already where it needs to
/// press. Splitting them puts a glance between the person and the only thing
/// they came to do.
///
/// The time shown is the server's. A phone running fast would otherwise display
/// one time and record another, and the employee would be right to call that a
/// lie.
///
/// **Held, not tapped.** A punch is a legal record that cannot be undone — the
/// row is append-only in the database, and a correction afterwards costs a
/// manager's time. A brief hold is the difference between deciding and
/// brushing the screen with a glove, and the ring filling under the thumb says
/// how much longer without a word.
class PunchClockButton extends StatefulWidget {
  const PunchClockButton({
    required this.dateLabel,
    required this.timeLabel,
    required this.onPressed,
    super.key,
    this.isLoading = false,
    this.compact = false,
    this.holdDuration = const Duration(milliseconds: 900),
  });

  /// Already formatted for a person — "Sex, 30 Ago 2026".
  final String dateLabel;

  /// The official time. Shows placeholder dashes until the clock has synced,
  /// because falling back to the device is the failure this design prevents.
  final String timeLabel;

  final VoidCallback? onPressed;
  final bool isLoading;
  final bool compact;

  /// How long the thumb stays down before the punch is recorded.
  ///
  /// Long enough to be a decision, short enough that somebody standing in the
  /// rain does not resent it.
  final Duration holdDuration;

  @override
  State<PunchClockButton> createState() => _PunchClockButtonState();
}

class _PunchClockButtonState extends State<PunchClockButton>
    with SingleTickerProviderStateMixin {
  late final AnimationController _hold = AnimationController(
    vsync: this,
    duration: widget.holdDuration,
  )..addStatusListener(_onHoldStatus);

  bool get _isEnabled => widget.onPressed != null && !widget.isLoading;

  void _onHoldStatus(AnimationStatus status) {
    if (status == AnimationStatus.completed) {
      widget.onPressed?.call();

      // Reset so a second punch starts from empty rather than from a full ring
      // that looks like it already fired.
      _hold.value = 0;
    }
  }

  void _startHold() {
    if (_isEnabled) {
      _hold.forward();
    }
  }

  /// Runs the ring back rather than snapping it to zero, so a hold abandoned
  /// halfway reads as cancelled instead of as a glitch.
  void _cancelHold() {
    if (_hold.status == AnimationStatus.forward) {
      _hold.reverse();
    }
  }

  @override
  void didUpdateWidget(PunchClockButton oldWidget) {
    super.didUpdateWidget(oldWidget);

    if (!_isEnabled) {
      _hold.value = 0;
    }
  }

  @override
  void dispose() {
    _hold.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Semantics(
      button: true,
      enabled: _isEnabled,
      label: 'Marcar ponto. ${widget.dateLabel}, ${widget.timeLabel}',

      // A screen reader user cannot hold a button they cannot see. The
      // accessibility action fires straight away — the hold guards against a
      // brushed screen, and that is not how this control is reached.
      onTap: _isEnabled ? widget.onPressed : null,
      child: ExcludeSemantics(
        child: GestureDetector(
          onTapDown: (_) => _startHold(),
          onTapUp: (_) => _cancelHold(),
          onTapCancel: _cancelHold,
          child: Material(
            color: _isEnabled
                ? AppColors.primary
                : AppColors.primary.withValues(alpha: 0.55),
            borderRadius: BorderRadius.circular(AppRadius.lg),
            child: Padding(
              padding: EdgeInsets.symmetric(
                horizontal: AppSpacing.lg,
                vertical: widget.compact ? AppSpacing.sm + 4 : AppSpacing.md,
              ),
              child: Row(
                children: [
                  Expanded(child: _Clock(button: widget)),
                  const SizedBox(width: AppSpacing.md),
                  _HoldIndicator(
                    progress: _hold,
                    isLoading: widget.isLoading,
                    size: widget.compact ? 30 : 36,
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}

/// The date, the time, and what the control does.
class _Clock extends StatelessWidget {
  const _Clock({required this.button});

  final PunchClockButton button;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      mainAxisSize: MainAxisSize.min,
      children: [
        Text(
          button.dateLabel,
          style: AppTypography.labelSmall.copyWith(
            color: AppColors.onPrimary.withValues(alpha: 0.85),
          ),
        ),
        const SizedBox(height: AppSpacing.xs / 2),
        Text(
          button.timeLabel,
          style: AppTypography.displayLarge.copyWith(
            color: AppColors.onPrimary,
            fontSize: button.compact ? 28 : 34,
            fontWeight: FontWeight.w300,
            height: 1,
            fontFeatures: const [FontFeature.tabularFigures()],
          ),
        ),
        const SizedBox(height: AppSpacing.xs / 2),

        // Named on the control itself. An amber rectangle showing a clock is
        // not self-evidently the thing that records a punch, and the person
        // reading it may be doing so for the first time.
        Text(
          button.isLoading ? 'Registrando...' : 'Segure para marcar ponto',
          key: const Key('punch_button_hint'),
          style: AppTypography.labelSmall.copyWith(
            color: AppColors.onPrimary.withValues(alpha: 0.8),
            fontWeight: FontWeight.w600,
          ),
        ),
      ],
    );
  }
}

/// The ring that fills while the thumb is down.
class _HoldIndicator extends StatelessWidget {
  const _HoldIndicator({
    required this.progress,
    required this.isLoading,
    required this.size,
  });

  final Animation<double> progress;
  final bool isLoading;
  final double size;

  @override
  Widget build(BuildContext context) {
    return SizedBox.square(
      dimension: size,
      child: isLoading
          // Once the request is in flight the hold is over and the wait is
          // somebody else's, so the ring stops measuring and starts spinning.
          ? const CircularProgressIndicator(
              key: Key('punch_button_sending'),
              strokeWidth: 2.6,
              color: AppColors.onPrimary,
            )
          : AnimatedBuilder(
              animation: progress,
              builder: (context, _) => Stack(
                alignment: Alignment.center,
                children: [
                  CircularProgressIndicator(
                    key: const Key('punch_button_hold'),
                    value: progress.value,
                    strokeWidth: 2.6,
                    color: AppColors.onPrimary,
                    backgroundColor: AppColors.onPrimary.withValues(
                      alpha: 0.25,
                    ),
                  ),
                  Icon(
                    // A clock, not a fingerprint. The product confirms by
                    // location and reads no biometrics.
                    Icons.schedule_outlined,
                    color: AppColors.onPrimary.withValues(
                      alpha: 1 - (progress.value * 0.6),
                    ),
                    size: size * 0.55,
                  ),
                ],
              ),
            ),
    );
  }
}
