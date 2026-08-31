import 'package:nahora/features/auth/domain/authenticated_user.dart';
import 'package:nahora/features/auth/domain/permissions.dart';
import 'package:nahora/features/auth/domain/session.dart';

/// A signed-in person, for screens that render differently depending on who is
/// looking.
///
/// Permissions are passed in rather than derived from a role name: the app
/// checks permissions, and a fake that keyed off the role would let a screen
/// pass a test it fails in production.
Session sessionFor({
  String fullName = 'Joana Funcionária',
  List<String> permissions = const [Permissions.viewOwnAttendance],
  String? employeeId = 'employee-1',
}) => Session(
  user: AuthenticatedUser(
    id: 'user-1',
    fullName: fullName,
    role: 'Employee',
    employeeId: employeeId,
    permissions: permissions,
  ),
  pendingActions: const [],
  expiresAt: DateTime.utc(2026, 12, 31),
);

/// Somebody who may decide other people's corrections.
Session managerSession({String fullName = 'Marina Gestora'}) => sessionFor(
  fullName: fullName,
  permissions: const [
    Permissions.viewOwnAttendance,
    Permissions.registerAttendance,
    Permissions.viewAllAttendance,
    Permissions.adjustAttendance,
    Permissions.viewEmployees,
  ],
);
