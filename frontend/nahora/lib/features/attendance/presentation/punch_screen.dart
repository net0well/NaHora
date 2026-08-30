import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../design_system/components/app_brand_band.dart';
import '../../../design_system/components/app_button.dart';
import '../../../design_system/components/app_button_variant.dart';
import '../../../design_system/components/app_location_preview.dart';
import '../../../design_system/components/punch_clock_button.dart';
import '../../../design_system/theme/app_colors.dart';
import '../../../design_system/theme/app_spacing.dart';
import '../../../design_system/theme/app_typography.dart';
import '../../auth/presentation/auth_providers.dart';
import '../domain/punch_receipt.dart';
import 'attendance_providers.dart';
import 'punch_state.dart';
import 'server_clock_controller.dart';

/// The screen the product exists for.
///
/// Structure follows the approved design: a brand band naming who is punching
/// and for which company, the location above, and the clock living **inside**
/// the action rather than beside it — so the employee looks at the time with
/// their thumb already where it needs to press.
class PunchScreen extends ConsumerWidget {
  const PunchScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(punchControllerProvider);

    return Scaffold(
      backgroundColor: AppColors.background,
      body: SafeArea(
        bottom: false,
        child: Column(
          children: [
            const _Band(),
            Expanded(
              child: Padding(
                padding: const EdgeInsets.all(AppSpacing.lg),
                child: state is PunchConfirmed
                    ? _Confirmation(receipt: state.receipt)
                    : _PunchFlow(state: state),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

/// Who is punching, and for whom.
///
/// The company line matters more than it looks: someone who works for two
/// employers through the same app needs to confirm at a glance that they are
/// about to punch for the right one.
class _Band extends ConsumerWidget {
  const _Band();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final session = ref.watch(currentSessionProvider);

    return AppBrandBand(
      eyebrow: 'Olá,',
      title: session?.user.fullName ?? 'Bem-vindo',
      onBack: () => _leave(context, ref),
    );
  }
}

/// Leaves the screen, clearing the punch state on the way out.
///
/// Without the reset, reopening the screen would show the previous
/// confirmation as though it had just happened.
void _leave(BuildContext context, WidgetRef ref) {
  ref.read(punchControllerProvider.notifier).reset();
  if (context.canPop()) {
    context.pop();
  }
}

/// Location, then the clock button. Nothing between them.
class _PunchFlow extends StatelessWidget {
  const _PunchFlow({required this.state});

  final PunchState state;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        AppLocationPreview(
          height: 190,
          accuracyMeters: switch (state) {
            PunchReady(:final location) => location.accuracyMeters,
            PunchLowAccuracy(:final location) => location.accuracyMeters,
            _ => null,
          },
          isDegraded: state is PunchLowAccuracy,
          isUnavailable:
              state is PunchLocationUnavailable ||
              state is PunchLocating ||
              state is PunchPermissionDenied,
        ),
        const SizedBox(height: AppSpacing.md),
        _StateMessage(state: state),
        const Spacer(),
        const _ClockButton(),
      ],
    );
  }
}

/// The one line that explains what the screen is doing, or why it cannot.
class _StateMessage extends StatelessWidget {
  const _StateMessage({required this.state});

  final PunchState state;

  @override
  Widget build(BuildContext context) {
    final (colour, message) = switch (state) {
      PunchLocating() => (
        AppColors.textSecondary,
        'Obtendo sua localização...',
      ),
      PunchReady() => (AppColors.success, 'Tudo pronto para marcar'),
      PunchLowAccuracy() => (
        AppColors.warning,
        'A precisão está baixa. Você pode marcar assim mesmo.',
      ),
      PunchLocationUnavailable() => (
        AppColors.warning,
        'Sem sinal de GPS. A marcação será registrada assim mesmo.',
      ),
      PunchPermissionDenied(:final isPermanent) => (
        AppColors.danger,
        isPermanent
            ? 'Libere a localização nas configurações do Android para marcar o ponto.'
            : 'Permita o acesso à localização para marcar o ponto.',
      ),
      PunchSubmitting() => (AppColors.textSecondary, 'Registrando...'),
      PunchFailed(:final message) => (AppColors.danger, message),
      PunchConfirmed() => (AppColors.success, 'Ponto registrado'),
    };

    return Semantics(
      liveRegion: true,
      label: message,
      child: ExcludeSemantics(
        child: Text(
          message,
          key: const Key('punch_state_message'),
          textAlign: TextAlign.center,
          style: AppTypography.bodyMedium.copyWith(
            color: colour,
            fontWeight: FontWeight.w600,
          ),
        ),
      ),
    );
  }
}

/// The clock and the action, ticking once a second.
class _ClockButton extends ConsumerStatefulWidget {
  const _ClockButton();

  @override
  ConsumerState<_ClockButton> createState() => _ClockButtonState();
}

class _ClockButtonState extends ConsumerState<_ClockButton> {
  Timer? _ticker;

  @override
  void initState() {
    super.initState();

    // Repaints the label only. The clock itself advances from its monotonic
    // anchor whether or not anything is watching.
    _ticker = Timer.periodic(const Duration(seconds: 1), (_) {
      if (mounted) {
        setState(() {});
      }
    });
  }

  @override
  void dispose() {
    _ticker?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(punchControllerProvider);
    final now = ref.watch(serverClockProvider).nowUtc();
    final offset = ref.read(serverClockControllerProvider.notifier).offset;

    final local = now?.add(offset);

    return PunchClockButton(
      key: const Key('punch_button'),
      dateLabel: local == null
          ? 'Obtendo a hora oficial'
          : formatPunchDate(local),
      timeLabel: local == null ? '--:--:--' : formatPunchTime(local),
      isLoading: state is PunchSubmitting,
      onPressed: state.canPunch
          ? () => ref.read(punchControllerProvider.notifier).punch()
          : null,
    );
  }
}

/// The confirmation, which has to leave no doubt.
///
/// A weak one produces the second punch "just in case", and a duplicate is
/// work for a manager afterwards.
class _Confirmation extends ConsumerWidget {
  const _Confirmation({required this.receipt});

  final PunchReceipt receipt;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final local = receipt.occurredAt.add(
      ref.read(serverClockControllerProvider.notifier).offset,
    );

    return Column(
      key: const Key('punch_confirmation'),
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        const Spacer(),
        const Icon(Icons.check_circle, size: 64, color: AppColors.success),
        const SizedBox(height: AppSpacing.md),
        Text(
          '${receipt.direction.label} registrada',
          textAlign: TextAlign.center,
          style: AppTypography.titleLarge,
        ),
        const SizedBox(height: AppSpacing.sm),
        Text(
          formatPunchTime(local).substring(0, 5),
          textAlign: TextAlign.center,
          style: AppTypography.displayLarge.copyWith(
            color: AppColors.success,
            fontWeight: FontWeight.w300,
            fontFeatures: const [FontFeature.tabularFigures()],
          ),
        ),
        const SizedBox(height: AppSpacing.sm),
        Text(
          'NSR ${receipt.nsr}',
          textAlign: TextAlign.center,
          style: AppTypography.labelSmall.copyWith(
            color: AppColors.textSecondary,
          ),
        ),
        const Spacer(),
        AppButton(
          key: const Key('punch_again'),
          label: 'Voltar',
          variant: AppButtonVariant.secondary,
          expanded: true,
          onPressed: () => _leave(context, ref),
        ),
      ],
    );
  }
}

/// "14:32:07"
String formatPunchTime(DateTime value) =>
    '${_pad(value.hour)}:${_pad(value.minute)}:${_pad(value.second)}';

/// "Sex, 30 Ago 2026"
String formatPunchDate(DateTime value) {
  const weekdays = ['Seg', 'Ter', 'Qua', 'Qui', 'Sex', 'Sáb', 'Dom'];
  const months = [
    'Jan',
    'Fev',
    'Mar',
    'Abr',
    'Mai',
    'Jun',
    'Jul',
    'Ago',
    'Set',
    'Out',
    'Nov',
    'Dez',
  ];

  return '${weekdays[value.weekday - 1]}, ${value.day} '
      '${months[value.month - 1]} ${value.year}';
}

String _pad(int value) => value.toString().padLeft(2, '0');
