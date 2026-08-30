/// Permission names, mirroring the server.
///
/// The strings are the contract. A typo here does not fail the build — it
/// silently hides a button, which is why they are declared once and never
/// written inline.
abstract final class Permissions {
  static const String viewOwnAttendance = 'attendance:view-own';
  static const String registerAttendance = 'attendance:register';
  static const String viewAllAttendance = 'attendance:view-all';
  static const String adjustAttendance = 'attendance:adjust';
  static const String viewEmployees = 'employees:view';
  static const String manageEmployees = 'employees:manage';
}
