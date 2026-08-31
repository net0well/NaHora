import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network_providers.dart';
import '../../auth/presentation/auth_providers.dart';
import '../data/adjustments_repository.dart';
import '../data/image_picker_attachment_picker.dart';
import '../domain/attachment_picker.dart';
import '../domain/adjustment.dart';
import '../domain/justification.dart';

final adjustmentsRepositoryProvider = Provider<AdjustmentsRepository>(
  (ref) => AdjustmentsRepository(client: ref.watch(apiClientProvider)),
);

/// The requests still waiting on a decision.
///
/// The manager's queue. For anybody else the server answers with their own
/// pending requests, which is what the employee's screen wants to show anyway —
/// the filtering is the server's job, not a parameter the app chooses.
///
/// Watches the session so the queue does not outlive a sign-out: the next
/// person on the same handset must not open the app on somebody else's
/// corrections.
final pendingAdjustmentsProvider = FutureProvider.autoDispose<List<Adjustment>>(
  (ref) {
    ref.watch(currentSessionProvider);

    return ref.watch(adjustmentsRepositoryProvider).list(pendingOnly: true);
  },
);

/// The catalogue of reasons, in the order the server wants them shown.
///
/// Not auto-disposed: it is a fixed list that does not change between two taps,
/// and refetching it every time a form opens asks the same question again.
final justificationsProvider = FutureProvider<List<Justification>>(
  (ref) => ref.watch(adjustmentsRepositoryProvider).justifications(),
);

/// The camera and gallery, behind a port so a test can stand in for them.
final attachmentPickerProvider = Provider<AttachmentPicker>(
  (ref) => ImagePickerAttachmentPicker(),
);
