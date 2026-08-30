import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network_providers.dart';
import '../../auth/presentation/auth_providers.dart';
import '../data/profile_repository.dart';
import '../domain/company_profile.dart';
import '../domain/employee_profile.dart';

final profileRepositoryProvider = Provider<ProfileRepository>(
  (ref) => ProfileRepository(client: ref.watch(apiClientProvider)),
);

/// The caller's own employment record.
///
/// Not auto-disposed: a profile does not change while somebody looks at a
/// punch screen, and refetching it on every return to the tab is a request
/// that answers the same thing.
///
/// It does watch the session, though. Without that the cached record outlives
/// a sign-out, and the next person to sign in on the same handset opens the
/// app on somebody else's CPF.
final employeeProfileProvider = FutureProvider<EmployeeProfile>((ref) {
  ref.watch(currentSessionProvider);

  return ref.watch(profileRepositoryProvider).employee();
});

/// The company the caller works for.
final companyProfileProvider = FutureProvider<CompanyProfile>((ref) {
  ref.watch(currentSessionProvider);

  return ref.watch(profileRepositoryProvider).company();
});
