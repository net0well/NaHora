import 'dart:math' as math;

import 'package:flutter/material.dart';

import '../theme/app_colors.dart';

/// The button the whole product exists for.
///
/// A live clock face rather than a static icon: the hands show the official
/// time and the second hand moves, so the control says "the time, now, and I am
/// the thing that records it" without a caption. A fingerprint would have been
/// the obvious shape and the wrong promise — this product reads no biometrics.
///
/// A slow ring breathes behind it. It is the only thing on screen that moves,
/// which is what makes the eye land there first on a phone held at the entrance
/// of a warehouse.
class PunchActionButton extends StatefulWidget {
  const PunchActionButton({
    required this.time,
    required this.onPressed,
    super.key,
    this.size = 70,
    this.isBusy = false,
    this.semanticLabel = 'Marcar ponto',
  });

  /// The official time, already in company hours.
  ///
  /// Null before the clock has synced. The face then shows no hands rather than
  /// the device's own time — falling back to it is the failure this whole
  /// mechanism exists to prevent.
  final DateTime? time;

  final VoidCallback? onPressed;
  final double size;
  final bool isBusy;
  final String semanticLabel;

  @override
  State<PunchActionButton> createState() => _PunchActionButtonState();
}

class _PunchActionButtonState extends State<PunchActionButton>
    with SingleTickerProviderStateMixin {
  late final AnimationController _pulse = AnimationController(
    vsync: this,
    duration: const Duration(seconds: 2),
  );

  bool get _isEnabled => widget.onPressed != null && !widget.isBusy;

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();

    // Respects the system's reduce-motion setting, and that is also what keeps
    // a repeating animation from hanging a widget test that settles.
    final wantsMotion = !MediaQuery.disableAnimationsOf(context) && _isEnabled;

    if (wantsMotion && !_pulse.isAnimating) {
      _pulse.repeat();
    } else if (!wantsMotion && _pulse.isAnimating) {
      _pulse.stop();
    }
  }

  @override
  void didUpdateWidget(PunchActionButton oldWidget) {
    super.didUpdateWidget(oldWidget);

    if (!_isEnabled && _pulse.isAnimating) {
      _pulse.stop();
    }
  }

  @override
  void dispose() {
    _pulse.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Semantics(
      button: true,
      enabled: _isEnabled,
      label: widget.semanticLabel,
      child: ExcludeSemantics(
        child: SizedBox.square(
          dimension: widget.size,
          child: Stack(
            alignment: Alignment.center,
            children: [
              // Behind the face, so the ring never sits over the hands.
              AnimatedBuilder(
                animation: _pulse,
                builder: (context, _) => CustomPaint(
                  size: Size.square(widget.size),
                  painter: _PulsePainter(progress: _pulse.value),
                ),
              ),
              Material(
                color: _isEnabled
                    ? AppColors.primary
                    : AppColors.primary.withValues(alpha: 0.45),
                shape: const CircleBorder(),
                elevation: 6,
                shadowColor: const Color(0xB3000000),
                child: InkWell(
                  onTap: _isEnabled ? widget.onPressed : null,
                  customBorder: const CircleBorder(),
                  child: SizedBox.square(
                    dimension: widget.size,
                    child: widget.isBusy
                        ? const Padding(
                            padding: EdgeInsets.all(20),
                            child: CircularProgressIndicator(
                              strokeWidth: 2.6,
                              color: AppColors.onPrimary,
                            ),
                          )
                        : CustomPaint(
                            key: const Key('punch_action_clock'),
                            painter: _ClockFacePainter(time: widget.time),
                          ),
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

/// The ring that breathes outward and fades.
class _PulsePainter extends CustomPainter {
  const _PulsePainter({required this.progress});

  final double progress;

  @override
  void paint(Canvas canvas, Size size) {
    if (progress == 0) {
      return;
    }

    final centre = Offset(size.width / 2, size.height / 2);
    final base = size.width / 2;

    // Grows to a fifth beyond the button and fades as it goes, so the edge
    // reads as a ripple rather than a border somebody might try to tap.
    final radius = base * (1 + (progress * 0.22));
    final opacity = (1 - progress) * 0.35;

    canvas.drawCircle(
      centre,
      radius,
      Paint()
        ..style = PaintingStyle.stroke
        ..strokeWidth = 2
        ..color = AppColors.primary.withValues(alpha: opacity),
    );
  }

  @override
  bool shouldRepaint(_PulsePainter oldDelegate) =>
      oldDelegate.progress != progress;
}

/// A clock face: twelve ticks and three hands.
class _ClockFacePainter extends CustomPainter {
  const _ClockFacePainter({required this.time});

  final DateTime? time;

  @override
  void paint(Canvas canvas, Size size) {
    final centre = Offset(size.width / 2, size.height / 2);
    final radius = size.width / 2;

    _paintTicks(canvas, centre, radius);

    // No hands until the official time is known. Drawing the device's own
    // would be the one lie this product must never tell.
    final clock = time;
    if (clock == null) {
      return;
    }

    final hourTurn = ((clock.hour % 12) + (clock.minute / 60)) / 12;
    final minuteTurn = (clock.minute + (clock.second / 60)) / 60;
    final secondTurn = clock.second / 60;

    _paintHand(
      canvas,
      centre,
      radius * 0.42,
      hourTurn,
      3.2,
      AppColors.onPrimary,
    );
    _paintHand(
      canvas,
      centre,
      radius * 0.60,
      minuteTurn,
      2.4,
      AppColors.onPrimary,
    );

    // Thinner and lighter: it moves, so it needs no weight to be noticed.
    _paintHand(
      canvas,
      centre,
      radius * 0.62,
      secondTurn,
      1.2,
      AppColors.onPrimary.withValues(alpha: 0.65),
    );

    canvas.drawCircle(centre, 2.2, Paint()..color = AppColors.onPrimary);
  }

  void _paintTicks(Canvas canvas, Offset centre, double radius) {
    final paint = Paint()
      ..color = AppColors.onPrimary.withValues(alpha: 0.3)
      ..strokeCap = StrokeCap.round;

    for (var hour = 0; hour < 12; hour++) {
      final angle = (hour / 12) * 2 * math.pi - (math.pi / 2);

      // The quarters are longer, which is what makes the face readable at this
      // size without numbers.
      final isQuarter = hour % 3 == 0;
      paint.strokeWidth = isQuarter ? 2 : 1;

      final outer = radius * 0.82;
      final inner = radius * (isQuarter ? 0.70 : 0.75);

      canvas.drawLine(
        centre + Offset(math.cos(angle) * inner, math.sin(angle) * inner),
        centre + Offset(math.cos(angle) * outer, math.sin(angle) * outer),
        paint,
      );
    }
  }

  void _paintHand(
    Canvas canvas,
    Offset centre,
    double length,
    double turn,
    double width,
    Color colour,
  ) {
    // Twelve o'clock is up, and angles start at three — hence the quarter turn.
    final angle = (turn * 2 * math.pi) - (math.pi / 2);

    canvas.drawLine(
      centre,
      centre + Offset(math.cos(angle) * length, math.sin(angle) * length),
      Paint()
        ..color = colour
        ..strokeWidth = width
        ..strokeCap = StrokeCap.round,
    );
  }

  @override
  bool shouldRepaint(_ClockFacePainter oldDelegate) => oldDelegate.time != time;
}
