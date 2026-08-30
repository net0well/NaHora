import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network_providers.dart';
import '../../auth/presentation/auth_providers.dart';
import '../data/adjustments_repository.dart';
import '../domain/adjustment.dart';

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
