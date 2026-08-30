/// The signed-in person, as the UI needs them.
class AuthenticatedUser {
  const AuthenticatedUser({
    required this.id,
    required this.fullName,
    required this.role,
    required this.permissions,
    this.employeeId,
  });

  final String id;
  final String fullName;
  final String role;

  /// Null for someone with no employment record — an administrator who never
  /// punches. The punch screen keys off this rather than off the role.
  final String? employeeId;

  /// What the UI may offer. Hiding a button the user cannot use is courtesy;
  /// the server decides regardless.
  final List<String> permissions;

  bool can(String permission) => permissions.contains(permission);
}
