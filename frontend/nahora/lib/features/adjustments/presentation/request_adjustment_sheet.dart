import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../design_system/components/app_button.dart';
import '../../../design_system/components/app_button_variant.dart';
import '../../../design_system/components/app_error_state.dart';
import '../../../design_system/components/app_loading.dart';
import '../../../design_system/components/app_text_field.dart';
import '../../../design_system/theme/app_colors.dart';
import '../../../design_system/theme/app_radius.dart';
import '../../../design_system/theme/app_spacing.dart';
import '../../../design_system/theme/app_typography.dart';
import '../../attendance/domain/punch_direction.dart';
import '../../attendance/domain/punch_entry.dart';
import '../../attendance/presentation/punch_entry_formatting.dart';
import '../domain/attachment_source.dart';
import '../domain/justification.dart';
import 'adjustments_providers.dart';
import 'request_adjustment_controller.dart';

/// Asking for a day's record to be corrected.
///
/// Nothing here changes a mark. It sends a request that a manager decides, and
/// until they do the original stands exactly as it was recorded.
class RequestAdjustmentSheet extends ConsumerStatefulWidget {
  const RequestAdjustmentSheet({
    required this.dayLabel,
    required this.localDate,
    required this.entries,
    super.key,
  });

  /// "30/08/2026", already formatted.
  final String dayLabel;

  /// "2026-08-30" — the working day the correction lands on.
  final String localDate;

  /// The day's marks, so a correction can name the one it replaces.
  final List<PunchEntry> entries;

  @override
  ConsumerState<RequestAdjustmentSheet> createState() =>
      _RequestAdjustmentSheetState();
}

class _RequestAdjustmentSheetState
    extends ConsumerState<RequestAdjustmentSheet> {
  static const String _inclusion = 'inclusion';

  final TextEditingController _notes = TextEditingController();

  /// A mark's id, or [_inclusion] for one that was never made.
  late String _target = _correctable.isEmpty
      ? _inclusion
      : _correctable.first.id;

  TimeOfDay? _time;
  PunchDirection _direction = PunchDirection.entry;
  Justification? _justification;
  String? _attachmentPath;

  /// Marks that can be corrected. An inclusion has no punch behind it, so there
  /// is nothing for a correction to point at.
  List<PunchEntry> get _correctable =>
      widget.entries.where((entry) => !entry.isIncluded).toList();

  bool get _isInclusion => _target == _inclusion;

  bool get _needsNotes => _justification?.requiresNotes ?? false;

  bool get _canSubmit =>
      _time != null &&
      _justification != null &&
      (!_needsNotes || _notes.text.trim().isNotEmpty);

  @override
  void initState() {
    super.initState();

    // Prefilled with the time of the mark being corrected: somebody asking for
    // 08:05 instead of 08:32 should be typing one change, not both.
    final first = _correctable.isEmpty ? null : _correctable.first;
    _time = first == null ? null : _timeOf(first);
  }

  @override
  void dispose() {
    _notes.dispose();
    super.dispose();
  }

  static TimeOfDay _timeOf(PunchEntry entry) {
    final local = entry.occurredAt.add(entry.companyOffset);

    return TimeOfDay(hour: local.hour, minute: local.minute);
  }

  /// The chosen hour on the chosen day, in UTC.
  ///
  /// Built from the company offset the marks carry rather than the device's own
  /// timezone: a phone set to another one would ask for a correction an hour
  /// away from the one the person typed.
  DateTime _occurredAtUtc() {
    final parts = widget.localDate.split('-').map(int.parse).toList();
    final offset = widget.entries.isEmpty
        ? Duration.zero
        : widget.entries.first.companyOffset;

    final local = DateTime.utc(
      parts[0],
      parts[1],
      parts[2],
      _time!.hour,
      _time!.minute,
    );

    return local.subtract(offset);
  }

  Future<void> _pickTime() async {
    final chosen = await showTimePicker(
      context: context,
      initialTime: _time ?? TimeOfDay.now(),
    );

    if (chosen != null) {
      setState(() => _time = chosen);
    }
  }

  Future<void> _pickAttachment(AttachmentSource source) async {
    final path = await ref.read(attachmentPickerProvider).pick(source);

    // Null means they backed out of the camera, which is not a failure and
    // must not clear a file they had already chosen.
    if (path != null) {
      setState(() => _attachmentPath = path);
    }
  }

  Future<void> _submit() async {
    await ref
        .read(requestAdjustmentControllerProvider.notifier)
        .submit(
          justificationId: _justification!.id,
          occurredAt: _occurredAtUtc(),
          punchId: _isInclusion ? null : _target,
          direction: _isInclusion ? _direction.code : null,
          notes: _notes.text.trim().isEmpty ? null : _notes.text.trim(),
          attachmentPath: _attachmentPath,
        );
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(requestAdjustmentControllerProvider);
    final catalogue = ref.watch(justificationsProvider);

    if (state.isSent) {
      return _Sent(attachmentFailed: state.attachmentFailed);
    }

    return SafeArea(
      child: Padding(
        padding: EdgeInsets.only(
          left: AppSpacing.lg,
          right: AppSpacing.lg,
          top: AppSpacing.lg,

          // Above the keyboard, which otherwise covers the field being typed in
          // and the button underneath it.
          bottom: AppSpacing.lg + MediaQuery.viewInsetsOf(context).bottom,
        ),
        child: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Semantics(
                header: true,
                child: Text(
                  'Solicitar ajuste · ${widget.dayLabel}',
                  style: AppTypography.titleMedium.copyWith(
                    color: AppColors.textPrimary,
                  ),
                ),
              ),
              const SizedBox(height: AppSpacing.lg),

              _Label(text: 'Qual marcação'),
              _Targets(
                entries: _correctable,
                selected: _target,
                onSelected: (value) => setState(() {
                  _target = value;
                  final entry = _correctable
                      .where((candidate) => candidate.id == value)
                      .firstOrNull;
                  if (entry != null) {
                    _time = _timeOf(entry);
                  }
                }),
              ),

              if (_isInclusion) ...[
                const SizedBox(height: AppSpacing.lg),
                _Label(text: 'Qual batida'),
                _Directions(
                  selected: _direction,
                  onSelected: (value) => setState(() => _direction = value),
                ),
              ],

              const SizedBox(height: AppSpacing.lg),
              _Label(text: 'Horário'),
              _TimeField(time: _time, onTap: _pickTime),

              const SizedBox(height: AppSpacing.lg),
              _Label(text: 'Motivo'),
              catalogue.when(
                loading: () => const AppLoading(message: 'Carregando motivos'),
                error: (error, _) => AppErrorState(
                  message: 'Não foi possível carregar os motivos.',
                  onRetry: () => ref.invalidate(justificationsProvider),
                ),
                data: (value) => _Justifications(
                  catalogue: value,
                  selected: _justification,
                  onSelected: (value) => setState(() => _justification = value),
                ),
              ),

              const SizedBox(height: AppSpacing.lg),
              AppTextField(
                key: const Key('request_notes'),
                label: _needsNotes ? 'Observação (obrigatória)' : 'Observação',
                placeholder: 'O que aconteceu',
                controller: _notes,
                maxLines: 3,
                onChanged: (_) => setState(() {}),
              ),

              const SizedBox(height: AppSpacing.lg),
              _Label(text: 'Comprovante'),
              _Attachment(
                path: _attachmentPath,
                onPick: _pickAttachment,
                onRemove: () => setState(() => _attachmentPath = null),
              ),

              if (state.errorMessage != null) ...[
                const SizedBox(height: AppSpacing.md),
                _Failure(message: state.errorMessage!),
              ],

              const SizedBox(height: AppSpacing.lg),
              AppButton(
                key: const Key('request_submit'),
                label: 'Enviar pedido',
                expanded: true,
                isLoading: state.isSubmitting,
                onPressed: _canSubmit ? _submit : null,
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _Label extends StatelessWidget {
  const _Label({required this.text});

  final String text;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: AppSpacing.sm),
      child: Text(
        text,
        style: AppTypography.labelSmall.copyWith(
          color: AppColors.textSecondary,
        ),
      ),
    );
  }
}

/// Which mark the correction is about.
class _Targets extends StatelessWidget {
  const _Targets({
    required this.entries,
    required this.selected,
    required this.onSelected,
  });

  final List<PunchEntry> entries;
  final String selected;
  final ValueChanged<String> onSelected;

  @override
  Widget build(BuildContext context) {
    return Wrap(
      spacing: AppSpacing.sm,
      runSpacing: AppSpacing.sm,
      children: [
        for (final entry in entries)
          _Chip(
            key: Key('request_target_${entry.id}'),
            label: entry.timeLabel,
            isSelected: entry.id == selected,
            onTap: () => onSelected(entry.id),
          ),
        _Chip(
          key: const Key('request_target_inclusion'),
          label: 'Incluir marcação',
          isSelected: selected == _RequestAdjustmentSheetState._inclusion,
          onTap: () => onSelected(_RequestAdjustmentSheetState._inclusion),
        ),
      ],
    );
  }
}

/// Which boundary of the day an included mark is.
class _Directions extends StatelessWidget {
  const _Directions({required this.selected, required this.onSelected});

  final PunchDirection selected;
  final ValueChanged<PunchDirection> onSelected;

  @override
  Widget build(BuildContext context) {
    return Wrap(
      spacing: AppSpacing.sm,
      runSpacing: AppSpacing.sm,
      children: [
        for (final direction in PunchDirection.values)
          _Chip(
            key: Key('request_direction_${direction.code}'),
            label: direction.label,
            isSelected: direction == selected,
            onTap: () => onSelected(direction),
          ),
      ],
    );
  }
}

class _Justifications extends StatelessWidget {
  const _Justifications({
    required this.catalogue,
    required this.selected,
    required this.onSelected,
  });

  final List<Justification> catalogue;
  final Justification? selected;
  final ValueChanged<Justification> onSelected;

  @override
  Widget build(BuildContext context) {
    return Wrap(
      spacing: AppSpacing.sm,
      runSpacing: AppSpacing.sm,
      children: [
        for (final justification in catalogue)
          _Chip(
            key: Key('request_justification_${justification.code}'),
            label: justification.description,
            isSelected: justification.id == selected?.id,
            onTap: () => onSelected(justification),
          ),
      ],
    );
  }
}

class _TimeField extends StatelessWidget {
  const _TimeField({required this.time, required this.onTap});

  final TimeOfDay? time;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final label = time == null
        ? 'Escolher horário'
        : '${_pad(time!.hour)}:${_pad(time!.minute)}';

    return Align(
      alignment: Alignment.centerLeft,
      child: AppButton(
        key: const Key('request_time'),
        label: label,
        icon: Icons.schedule_outlined,
        variant: AppButtonVariant.secondary,
        onPressed: onTap,
      ),
    );
  }

  static String _pad(int value) => value.toString().padLeft(2, '0');
}

/// The optional document.
class _Attachment extends StatelessWidget {
  const _Attachment({
    required this.path,
    required this.onPick,
    required this.onRemove,
  });

  final String? path;
  final ValueChanged<AttachmentSource> onPick;
  final VoidCallback onRemove;

  @override
  Widget build(BuildContext context) {
    if (path case final chosen?) {
      return Row(
        children: [
          const Icon(
            Icons.check_circle_outline,
            size: 18,
            color: AppColors.success,
          ),
          const SizedBox(width: AppSpacing.sm),
          Expanded(
            child: Text(
              chosen.split(RegExp(r'[/\\]')).last,
              key: const Key('request_attachment_name'),
              overflow: TextOverflow.ellipsis,
              style: AppTypography.bodyMedium.copyWith(
                color: AppColors.textPrimary,
              ),
            ),
          ),
          IconButton(
            key: const Key('request_attachment_remove'),
            onPressed: onRemove,
            tooltip: 'Remover comprovante',
            icon: const Icon(Icons.close, color: AppColors.textSecondary),
          ),
        ],
      );
    }

    // Stacked rather than side by side: the two labels together are wider than
    // a phone leaves once the sheet has its padding.
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        AppButton(
          key: const Key('request_attachment_camera'),
          label: 'Tirar foto',
          icon: Icons.photo_camera_outlined,
          variant: AppButtonVariant.secondary,
          expanded: true,
          onPressed: () => onPick(AttachmentSource.camera),
        ),
        const SizedBox(height: AppSpacing.sm),
        AppButton(
          key: const Key('request_attachment_gallery'),
          label: 'Escolher da galeria',
          icon: Icons.image_outlined,
          variant: AppButtonVariant.secondary,
          expanded: true,
          onPressed: () => onPick(AttachmentSource.gallery),
        ),
      ],
    );
  }
}

/// What the sheet becomes once the request is in.
class _Sent extends StatelessWidget {
  const _Sent({required this.attachmentFailed});

  final bool attachmentFailed;

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.lg),
        child: Column(
          key: const Key('request_sent'),
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const Icon(
              Icons.mark_email_read_outlined,
              size: 36,
              color: AppColors.success,
            ),
            const SizedBox(height: AppSpacing.md),
            Text(
              'Pedido enviado',
              textAlign: TextAlign.center,
              style: AppTypography.titleMedium.copyWith(
                color: AppColors.textPrimary,
              ),
            ),
            const SizedBox(height: AppSpacing.sm),
            Text(
              attachmentFailed
                  // Said plainly, because the request did go through: somebody
                  // told their pedido failed sends it twice.
                  ? 'O pedido foi registrado, mas o comprovante não subiu. '
                        'Você pode anexá-lo depois.'
                  : 'Seu gestor vai analisar. Enquanto isso, a marcação '
                        'original continua valendo.',
              textAlign: TextAlign.center,
              style: AppTypography.bodyMedium.copyWith(
                color: AppColors.textSecondary,
              ),
            ),
            const SizedBox(height: AppSpacing.lg),
            AppButton(
              key: const Key('request_close'),
              label: 'Fechar',
              variant: AppButtonVariant.secondary,
              expanded: true,
              onPressed: () => Navigator.of(context).pop(),
            ),
          ],
        ),
      ),
    );
  }
}

/// A selectable label.
class _Chip extends StatelessWidget {
  const _Chip({
    required this.label,
    required this.isSelected,
    required this.onTap,
    super.key,
  });

  final String label;
  final bool isSelected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Semantics(
      button: true,
      selected: isSelected,
      label: label,
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

/// The failure, announced rather than merely drawn.
class _Failure extends StatelessWidget {
  const _Failure({required this.message});

  final String message;

  @override
  Widget build(BuildContext context) {
    return Semantics(
      liveRegion: true,
      label: message,
      child: ExcludeSemantics(
        child: Container(
          key: const Key('request_error'),
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
