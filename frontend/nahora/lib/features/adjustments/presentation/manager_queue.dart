import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../design_system/components/app_badge.dart';
import '../../../design_system/components/app_badge_tone.dart';
import '../../../design_system/components/app_button.dart';
import '../../../design_system/components/app_button_variant.dart';
import '../../../design_system/components/app_card.dart';
import '../../../design_system/components/app_empty_state.dart';
import '../../../design_system/components/app_error_state.dart';
import '../../../design_system/components/app_loading.dart';
import '../../../design_system/theme/app_colors.dart';
import '../../../design_system/theme/app_radius.dart';
import '../../../design_system/theme/app_spacing.dart';
import '../../../design_system/theme/app_typography.dart';
import '../domain/adjustment.dart';
import 'adjustment_formatting.dart';
import 'adjustments_providers.dart';
import 'decision_controller.dart';

/// The requests waiting on this manager.
///
/// Nothing here changes a punch. The original row stays exactly as recorded —
/// an approval attaches a correction that governs from then on, which is what
/// keeps the signed record it belongs to verifiable.
class ManagerQueue extends ConsumerWidget {
  const ManagerQueue({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final pending = ref.watch(pendingAdjustmentsProvider);
    final decision = ref.watch(decisionControllerProvider);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        if (decision.errorMessage != null) ...[
          _DecisionError(message: decision.errorMessage!),
          const SizedBox(height: AppSpacing.md),
        ],
        pending.when(
          loading: () =>
              const AppLoading(message: 'Carregando as solicitações'),
          error: (error, _) => AppErrorState(
            message: 'Não foi possível carregar as solicitações.',
            onRetry: () => ref.invalidate(pendingAdjustmentsProvider),
          ),
          data: (value) => _Queue(requests: value),
        ),
      ],
    );
  }
}

class _Queue extends StatelessWidget {
  const _Queue({required this.requests});

  final List<Adjustment> requests;

  @override
  Widget build(BuildContext context) {
    if (requests.isEmpty) {
      return const AppEmptyState(
        key: Key('manager_queue_empty'),
        icon: Icons.inbox_outlined,
        title: 'Nenhuma solicitação pendente',
        message:
            'Quando alguém pedir um ajuste no ponto, o pedido aparece aqui '
            'para você aprovar ou recusar.',
      );
    }

    return Column(
      key: const Key('manager_queue_list'),
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        for (var index = 0; index < requests.length; index++) ...[
          if (index > 0) const SizedBox(height: AppSpacing.md),
          _RequestCard(request: requests[index]),
        ],
      ],
    );
  }
}

/// One request, with everything a decision needs on the same card.
///
/// Whose record, which punch, what it becomes and why. A manager who has to
/// open something else to find out ends up deciding on the name alone.
class _RequestCard extends ConsumerWidget {
  const _RequestCard({required this.request});

  final Adjustment request;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final decision = ref.watch(decisionControllerProvider);
    final isDeciding = decision.isDeciding(request.id);

    return AppCard(
      key: Key('adjustment_card_${request.id}'),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              Expanded(
                child: Text(
                  request.employeeName,
                  style: AppTypography.bodyLarge.copyWith(
                    color: AppColors.textPrimary,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ),
              const SizedBox(width: AppSpacing.sm),
              const AppBadge(label: 'Pendente', tone: AppBadgeTone.warning),
            ],
          ),
          const SizedBox(height: AppSpacing.xs),
          Text(
            request.subjectLabel,
            style: AppTypography.labelSmall.copyWith(
              color: AppColors.textSecondary,
            ),
          ),
          const SizedBox(height: AppSpacing.md),

          // The day and the change together: a time with no date is a decision
          // made without knowing which day it lands on.
          Text(
            '${request.dayLabel} · ${request.changeLabel}',
            style: AppTypography.bodyMedium.copyWith(
              color: AppColors.textPrimary,
            ),
          ),
          const SizedBox(height: AppSpacing.sm),
          Text(
            request.justificationDescription,
            style: AppTypography.bodyMedium.copyWith(
              color: AppColors.textSecondary,
            ),
          ),

          if (request.notes case final notes?) ...[
            const SizedBox(height: AppSpacing.sm),
            _Notes(text: notes),
          ],

          if (request.hasAttachment) ...[
            const SizedBox(height: AppSpacing.sm),
            const _AttachmentHint(),
          ],

          const SizedBox(height: AppSpacing.sm),
          Text(
            'Solicitado por ${request.requestedByName} em '
            '${request.requestedAtLabel}',
            style: AppTypography.labelSmall.copyWith(
              color: AppColors.textSecondary,
            ),
          ),

          const SizedBox(height: AppSpacing.md),
          _Decision(
            request: request,
            isDeciding: isDeciding,
            isBlocked: decision.isBusy && !isDeciding,
          ),
        ],
      ),
    );
  }
}

/// What the employee wrote, set apart from what the app wrote.
class _Notes extends StatelessWidget {
  const _Notes({required this.text});

  final String text;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(AppSpacing.sm),
      decoration: BoxDecoration(
        color: AppColors.surfaceMuted,
        borderRadius: BorderRadius.circular(AppRadius.sm),
      ),
      child: Text(
        text,
        style: AppTypography.bodyMedium.copyWith(
          color: AppColors.textPrimary,
          fontStyle: FontStyle.italic,
        ),
      ),
    );
  }
}

/// Says a file exists, never where it is.
///
/// The link is fetched through its own authorised route, so a queue cannot
/// become a way to enumerate somebody's medical certificates.
class _AttachmentHint extends StatelessWidget {
  const _AttachmentHint();

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        const Icon(Icons.attach_file, size: 16, color: AppColors.textSecondary),
        const SizedBox(width: AppSpacing.xs),
        Text(
          'Comprovante anexado',
          style: AppTypography.labelSmall.copyWith(
            color: AppColors.textSecondary,
          ),
        ),
      ],
    );
  }
}

class _Decision extends ConsumerWidget {
  const _Decision({
    required this.request,
    required this.isDeciding,
    required this.isBlocked,
  });

  final Adjustment request;

  /// This card is the one being sent.
  final bool isDeciding;

  /// Another card is being sent. Both actions are held, so one tap in a list
  /// cannot become two decisions.
  final bool isBlocked;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    void decide({required bool approve}) => ref
        .read(decisionControllerProvider.notifier)
        .decide(adjustmentId: request.id, approve: approve);

    return Row(
      children: [
        Expanded(
          child: AppButton(
            key: Key('adjustment_approve_${request.id}'),
            label: 'Aprovar',
            isLoading: isDeciding,
            expanded: true,
            onPressed: isBlocked ? null : () => decide(approve: true),
          ),
        ),
        const SizedBox(width: AppSpacing.sm),
        Expanded(
          child: AppButton(
            key: Key('adjustment_reject_${request.id}'),
            label: 'Recusar',
            variant: AppButtonVariant.danger,
            expanded: true,
            onPressed: isDeciding || isBlocked
                ? null
                : () => decide(approve: false),
          ),
        ),
      ],
    );
  }
}

/// The failure, announced rather than merely drawn.
///
/// A manager who taps Aprovar and hears nothing has no way to know the request
/// was already decided by somebody else.
class _DecisionError extends StatelessWidget {
  const _DecisionError({required this.message});

  final String message;

  @override
  Widget build(BuildContext context) {
    return Semantics(
      liveRegion: true,
      label: message,
      child: ExcludeSemantics(
        child: Container(
          key: const Key('manager_queue_error'),
          padding: const EdgeInsets.all(AppSpacing.md),
          decoration: BoxDecoration(
            color: AppColors.dangerLight,
            borderRadius: BorderRadius.circular(AppRadius.sm),
          ),
          child: Row(
            children: [
              const Icon(
                Icons.error_outline,
                size: 18,
                color: AppColors.danger,
              ),
              const SizedBox(width: AppSpacing.sm),
              Expanded(
                child: Text(
                  message,
                  style: AppTypography.bodyMedium.copyWith(
                    color: AppColors.danger,
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
