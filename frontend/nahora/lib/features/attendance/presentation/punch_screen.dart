import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../design_system/components/app_button.dart';
import '../../../design_system/components/app_button_variant.dart';
import '../../../design_system/components/status_indicator.dart';
import '../../../design_system/components/status_tone.dart';
import '../../../design_system/theme/app_colors.dart';
import '../../../design_system/theme/app_radius.dart';
import '../../../design_system/theme/app_sizing.dart';
import '../../../design_system/theme/app_spacing.dart';
import '../../../design_system/theme/app_typography.dart';
import '../domain/punch_receipt.dart';
import 'attendance_providers.dart';
import 'punch_state.dart';
import 'server_clock_controller.dart';

/// The screen the product exists for.
///
/// Two things dominate it by design: the clock and the button. The clock
/// because the employee needs to see the time that will actually be recorded,
/// and the button because it is the only thing they came here to do.
class PunchScreen extends ConsumerWidget {
  const PunchScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(punchControllerProvider);

    return Scaffold(
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(AppSpacing.lg),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              const _ServerClockDisplay(),
              const Spacer(),
              _PunchButton(state: state),
              const SizedBox(height: AppSpacing.lg),
              _LocationBanner(state: state),
              const Spacer(),
            ],
          ),
        ),
      ),
    );
  }
}

/// The official time, ticking.
///
/// Shows the **server's** clock, never the device's. A phone running two hours
/// fast would otherwise display 16:32 while the punch it produces records
/// 14:32, and the employee would be right to think the app lied to them.
class _ServerClockDisplay extends ConsumerStatefulWidget {
  const _ServerClockDisplay();

  @override
  ConsumerState<_ServerClockDisplay> createState() =>
      _ServerClockDisplayState();
}

class _ServerClockDisplayState extends ConsumerState<_ServerClockDisplay> {
  Timer? _ticker;

  @override
  void initState() {
    super.initState();

    // Ticks the display only. The clock itself advances from its monotonic
    // anchor whether or not anyone is watching.
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
    final sync = ref.watch(serverClockControllerProvider);
    final clock = ref.watch(serverClockProvider);
    final now = clock.nowUtc();

    if (now == null) {
      return Column(
        key: const Key('punch_clock_unsynced'),
        children: [
          Text(
            '--:--:--',
            style: AppTypography.displayLarge.copyWith(
              color: AppColors.textDisabled,
            ),
          ),
          const SizedBox(height: AppSpacing.xs),
          Text(
            sync.hasError
                ? 'Sem conexão para obter a hora oficial'
                : 'Obtendo a hora oficial...',
            style: AppTypography.bodyMedium.copyWith(
              color: AppColors.textSecondary,
            ),
          ),
        ],
      );
    }

    final local = now.add(
      ref.read(serverClockControllerProvider.notifier).offset,
    );

    return Column(
      key: const Key('punch_clock'),
      children: [
        Text(
          _formatTime(local),
          style: AppTypography.displayLarge.copyWith(
            fontSize: 56,
            fontWeight: FontWeight.w300,
            fontFeatures: const [FontFeature.tabularFigures()],
          ),
        ),
        const SizedBox(height: AppSpacing.xs),
        Text(
          _formatDate(local),
          style: AppTypography.bodyMedium.copyWith(
            color: AppColors.textSecondary,
          ),
        ),
      ],
    );
  }

  static String _formatTime(DateTime value) =>
      '${_pad(value.hour)}:${_pad(value.minute)}:${_pad(value.second)}';

  static String _pad(int value) => value.toString().padLeft(2, '0');

  static String _formatDate(DateTime value) {
    const weekdays = [
      'segunda-feira',
      'terça-feira',
      'quarta-feira',
      'quinta-feira',
      'sexta-feira',
      'sábado',
      'domingo',
    ];
    const months = [
      'janeiro',
      'fevereiro',
      'março',
      'abril',
      'maio',
      'junho',
      'julho',
      'agosto',
      'setembro',
      'outubro',
      'novembro',
      'dezembro',
    ];

    return '${weekdays[value.weekday - 1]}, ${value.day} de ${months[value.month - 1]}';
  }
}

/// The largest interactive element on the screen, and always reachable without
/// scrolling on the smallest supported handset.
class _PunchButton extends ConsumerWidget {
  const _PunchButton({required this.state});

  final PunchState state;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    if (state is PunchConfirmed) {
      return _ConfirmationCard(receipt: (state as PunchConfirmed).receipt);
    }

    final isSubmitting = state is PunchSubmitting;

    return SizedBox(
      height: 120,
      child: AppButton(
        key: const Key('punch_button'),
        label: 'MARCAR PONTO',
        isLoading: isSubmitting,
        expanded: true,
        variant: AppButtonVariant.primary,
        onPressed: state.canPunch
            ? () => ref.read(punchControllerProvider.notifier).punch()
            : null,
      ),
    );
  }
}

/// What the app knows about the location, in words rather than a map.
///
/// The map is deliberately absent here: tiles cost money per load and the
/// employee already knows where they are. It belongs in the history, behind a
/// tap.
class _LocationBanner extends ConsumerWidget {
  const _LocationBanner({required this.state});

  final PunchState state;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final (tone, label) = switch (state) {
      PunchLocating() => (StatusTone.inProgress, 'Obtendo sua localização...'),
      PunchReady(:final location) => (
        StatusTone.success,
        'Localização confirmada · precisão ${location.accuracyMeters.round()} m',
      ),
      PunchLowAccuracy(:final location) => (
        StatusTone.warning,
        'Precisão baixa (${location.accuracyMeters.round()} m). Você pode marcar assim mesmo',
      ),
      PunchLocationUnavailable(:final reason) => (
        StatusTone.warning,
        '$reason A marcação será registrada assim mesmo',
      ),
      PunchPermissionDenied(:final isPermanent) => (
        StatusTone.danger,
        isPermanent
            ? 'Libere a localização nas configurações do Android para marcar o ponto'
            : 'Permita o acesso à localização para marcar o ponto',
      ),
      PunchSubmitting() => (StatusTone.inProgress, 'Registrando...'),
      PunchFailed(:final message) => (StatusTone.danger, message),
      PunchConfirmed() => (StatusTone.success, 'Ponto registrado'),
    };

    return Center(
      key: const Key('punch_location_banner'),
      child: StatusIndicator(label: label, tone: tone),
    );
  }
}

/// The confirmation, which has to be unambiguous.
///
/// A weak confirmation produces the second punch "just in case", and a
/// duplicate is work for a manager afterwards.
class _ConfirmationCard extends ConsumerWidget {
  const _ConfirmationCard({required this.receipt});

  final PunchReceipt receipt;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final local = receipt.occurredAt.add(
      ref.read(serverClockControllerProvider.notifier).offset,
    );

    return Container(
      key: const Key('punch_confirmation'),
      padding: const EdgeInsets.all(AppSpacing.lg),
      decoration: BoxDecoration(
        color: AppColors.successLight,
        borderRadius: BorderRadius.circular(AppRadius.md),
      ),
      child: Column(
        children: [
          const Icon(Icons.check_circle, size: 48, color: AppColors.success),
          const SizedBox(height: AppSpacing.sm),
          Text(
            '${receipt.direction.label} registrada às '
            '${local.hour.toString().padLeft(2, '0')}:'
            '${local.minute.toString().padLeft(2, '0')}',
            textAlign: TextAlign.center,
            style: AppTypography.titleMedium.copyWith(color: AppColors.success),
          ),
          const SizedBox(height: AppSpacing.md),
          SizedBox(
            height: AppSizing.buttonHeight,
            child: AppButton(
              key: const Key('punch_again'),
              label: 'Voltar',
              variant: AppButtonVariant.secondary,
              onPressed: () =>
                  ref.read(punchControllerProvider.notifier).reset(),
            ),
          ),
        ],
      ),
    );
  }
}
