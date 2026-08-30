import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../design_system/components/app_badge.dart';
import '../../../design_system/components/app_badge_tone.dart';
import '../../../design_system/components/app_button.dart';
import '../../../design_system/components/app_button_variant.dart';
import '../../../design_system/components/app_card.dart';
import '../../../design_system/components/app_detail_list.dart';
import '../../../design_system/components/app_empty_state.dart';
import '../../../design_system/components/app_error_state.dart';
import '../../../design_system/components/app_loading.dart';
import '../../../design_system/components/punch_entry_tile.dart';
import '../../../design_system/theme/app_colors.dart';
import '../../../design_system/theme/app_spacing.dart';
import '../../../design_system/theme/app_typography.dart';
import '../../attendance/domain/punch_day.dart';
import '../../attendance/presentation/attendance_providers.dart';
import '../../attendance/presentation/punch_entry_formatting.dart';
import '../../auth/presentation/auth_providers.dart';
import '../../profile/domain/company_profile.dart';
import '../../profile/domain/employee_profile.dart';
import '../../profile/presentation/profile_providers.dart';

/// Who the employee is, what they recorded today, and who they work for.
class EmployeeTab extends ConsumerWidget {
  const EmployeeTab({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final session = ref.watch(currentSessionProvider);

    return SafeArea(
      child: ListView(
        key: const Key('employee_tab_scroll'),
        padding: const EdgeInsets.all(AppSpacing.lg),
        children: [
          Text(
            'Olá,',
            style: AppTypography.bodyMedium.copyWith(
              color: AppColors.textSecondary,
            ),
          ),
          Semantics(
            header: true,
            child: Text(
              session?.user.fullName ?? 'Bem-vindo',
              style: AppTypography.headlineMedium.copyWith(
                color: AppColors.textPrimary,
              ),
            ),
          ),
          const SizedBox(height: AppSpacing.lg),
          const _TodaySection(),
          const SizedBox(height: AppSpacing.lg),
          const _EmployeeSection(),
          const SizedBox(height: AppSpacing.lg),
          const _CompanySection(),
          const SizedBox(height: AppSpacing.lg),
          const _SignOut(),
        ],
      ),
    );
  }
}

/// Today's punches.
class _TodaySection extends ConsumerWidget {
  const _TodaySection();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final day = ref.watch(punchDayProvider);

    return AppCard(
      title: 'Hoje',
      child: day.when(
        loading: () => const AppLoading(message: 'Carregando suas marcações'),
        error: (error, _) => AppErrorState(
          message: 'Não foi possível carregar suas marcações de hoje.',
          onRetry: () => ref.invalidate(punchDayProvider),
        ),
        data: (value) => _Today(day: value),
      ),
    );
  }
}

class _Today extends StatelessWidget {
  const _Today({required this.day});

  final PunchDay day;

  @override
  Widget build(BuildContext context) {
    if (day.isEmpty) {
      return const AppEmptyState(
        key: Key('employee_tab_empty'),
        icon: Icons.schedule_outlined,
        title: 'Nenhuma marcação ainda',
        message:
            'Suas marcações do dia aparecem aqui assim que você bater o '
            'primeiro ponto.',
      );
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text(
          // Named so the employee knows what the button will record before
          // pressing it, not after.
          'Próxima: ${day.nextDirection.label}',
          style: AppTypography.labelSmall.copyWith(color: AppColors.primary),
        ),
        const SizedBox(height: AppSpacing.sm),
        for (final entry in day.entries) ...[
          PunchEntryTile(
            key: Key('punch_entry_${entry.nsr}'),
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
  }
}

/// The employee's own record.
class _EmployeeSection extends ConsumerWidget {
  const _EmployeeSection();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final profile = ref.watch(employeeProfileProvider);

    return AppCard(
      title: 'Meus dados',
      child: profile.when(
        loading: () => const AppLoading(message: 'Carregando seus dados'),
        error: (error, _) => AppErrorState(
          message: 'Não foi possível carregar seus dados.',
          onRetry: () => ref.invalidate(employeeProfileProvider),
        ),
        data: (value) => _EmployeeDetails(profile: value),
      ),
    );
  }
}

class _EmployeeDetails extends StatelessWidget {
  const _EmployeeDetails({required this.profile});

  final EmployeeProfile profile;

  @override
  Widget build(BuildContext context) {
    return Column(
      key: const Key('employee_tab_profile'),
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        AppDetailList(
          entries: [
            AppDetail('Nome', profile.fullName),
            AppDetail('CPF', profile.cpf),
            AppDetail('PIS', profile.pis),
            AppDetail('Matrícula', profile.registration),
            AppDetail('Cargo', profile.position),
            AppDetail('Setor', profile.department),
            AppDetail('Contrato', profile.contractType.label),
            AppDetail('Situação', profile.status.label),
            AppDetail('Admissão', _asDate(profile.hiredAt)),
            AppDetail('Desligamento', _asDate(profile.terminatedAt)),
            AppDetail('Nascimento', _asDate(profile.birthDate)),
            AppDetail('E-mail', profile.email),
            AppDetail('Telefone', profile.phone),
            AppDetail('Endereço', profile.address?.singleLine),
          ],
        ),

        // Surfaced rather than hidden: the employee should learn what is
        // missing from their own record here, not when an export leaves them
        // off it.
        if (!profile.isExportableToAfd) ...[
          const SizedBox(height: AppSpacing.md),
          const Align(
            alignment: Alignment.centerLeft,
            child: AppBadge(label: 'PIS pendente', tone: AppBadgeTone.warning),
          ),
        ],
      ],
    );
  }
}

/// The company recording those hours.
class _CompanySection extends ConsumerWidget {
  const _CompanySection();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final company = ref.watch(companyProfileProvider);

    return AppCard(
      title: 'Empresa',
      child: company.when(
        loading: () => const AppLoading(message: 'Carregando a empresa'),
        error: (error, _) => AppErrorState(
          message: 'Não foi possível carregar os dados da empresa.',
          onRetry: () => ref.invalidate(companyProfileProvider),
        ),
        data: (value) => _CompanyDetails(company: value),
      ),
    );
  }
}

class _CompanyDetails extends StatelessWidget {
  const _CompanyDetails({required this.company});

  final CompanyProfile company;

  @override
  Widget build(BuildContext context) {
    return Column(
      key: const Key('employee_tab_company'),
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        AppDetailList(
          entries: [
            AppDetail('Nome fantasia', company.tradeName),
            AppDetail('Razão social', company.legalName),
            AppDetail('CNPJ', company.cnpj),
            AppDetail('Inscrição estadual', company.stateRegistration),
            AppDetail('Inscrição municipal', company.municipalRegistration),
            AppDetail('Fundação', _asDate(company.foundedOn)),
            AppDetail('E-mail', company.email),
            AppDetail('Telefone', company.phone),
            AppDetail('Endereço', company.address.singleLine),
            AppDetail('Fuso horário', company.timeZoneId),
          ],
        ),
        const SizedBox(height: AppSpacing.md),

        // Left-aligned rather than stretched: a badge sizes to its label, and
        // a full-width one reads as a banner.
        Align(
          alignment: Alignment.centerLeft,
          child: AppBadge(
            label: company.registryVerified
                ? 'CNPJ confirmado'
                : 'CNPJ não confirmado',
            tone: company.registryVerified
                ? AppBadgeTone.success
                : AppBadgeTone.neutral,
          ),
        ),
      ],
    );
  }
}

/// Leaving the app.
///
/// At the end of the scroll rather than in the header: signing out is rare and
/// destructive-feeling, and a control the thumb reaches by accident next to the
/// punch action is the wrong trade.
class _SignOut extends ConsumerWidget {
  const _SignOut();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return AppButton(
      key: const Key('employee_tab_sign_out'),
      label: 'Sair',
      variant: AppButtonVariant.secondary,
      expanded: true,
      onPressed: () => ref.read(authControllerProvider.notifier).signOut(),
    );
  }
}

/// "2026-08-30" as "30/08/2026".
String? _asDate(String? isoDate) {
  if (isoDate == null) {
    return null;
  }

  final parts = isoDate.split('-');

  return parts.length == 3 ? '${parts[2]}/${parts[1]}/${parts[0]}' : isoDate;
}
