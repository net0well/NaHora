import 'postal_address.dart';

/// The signed-in person's own employment record.
class EmployeeProfile {
  const EmployeeProfile({
    required this.id,
    required this.fullName,
    required this.cpf,
    required this.registration,
    required this.contractType,
    required this.status,
    required this.hiredAt,
    required this.isExportableToAfd,
    this.pis,
    this.position,
    this.department,
    this.terminatedAt,
    this.birthDate,
    this.email,
    this.phone,
    this.address,
  });

  factory EmployeeProfile.fromJson(Map<String, dynamic> json) =>
      EmployeeProfile(
        id: json['id'] as String,
        fullName: json['fullName'] as String,
        cpf: json['cpfFormatted'] as String? ?? json['cpf'] as String,
        pis: json['pis'] as String?,
        registration: json['registration'] as String,
        position: json['position'] as String?,
        department: json['department'] as String?,
        contractType: EmployeeContractType.fromCode(
          json['contractType'] as String,
        ),
        status: EmployeeProfileStatus.fromCode(json['status'] as String),
        hiredAt: json['hiredAt'] as String,
        terminatedAt: json['terminatedAt'] as String?,
        birthDate: json['birthDate'] as String?,
        email: json['email'] as String?,
        phone: json['phoneFormatted'] as String? ?? json['phone'] as String?,
        address: json['address'] == null
            ? null
            : PostalAddress.fromJson(json['address'] as Map<String, dynamic>),
        isExportableToAfd: json['isExportableToAfd'] as bool? ?? false,
      );

  final String id;
  final String fullName;
  final String cpf;
  final String? pis;

  /// Matrícula.
  final String registration;

  /// Cargo.
  final String? position;

  /// Setor.
  final String? department;

  final EmployeeContractType contractType;
  final EmployeeProfileStatus status;
  final String hiredAt;
  final String? terminatedAt;
  final String? birthDate;
  final String? email;
  final String? phone;
  final PostalAddress? address;

  /// False when the PIS is missing.
  ///
  /// Shown to the employee so they can see what is outstanding on their own
  /// record, rather than finding out when an export leaves them off it.
  final bool isExportableToAfd;
}

/// The kind of employment relationship.
enum EmployeeContractType {
  clt('Clt', 'CLT'),
  apprentice('Apprentice', 'Jovem aprendiz'),
  intern('Intern', 'Estágio'),
  temporary('Temporary', 'Temporário'),
  statutory('Statutory', 'Estatutário');

  const EmployeeContractType(this.code, this.label);

  final String code;
  final String label;

  /// Falls back to CLT for a value this build does not know, because a newer
  /// server adding one must not make somebody's own record unreadable.
  static EmployeeContractType fromCode(String code) => values.firstWhere(
    (type) => type.code == code,
    orElse: () => EmployeeContractType.clt,
  );
}

/// Where the employment relationship stands.
enum EmployeeProfileStatus {
  active('Active', 'Ativo'),
  onLeave('OnLeave', 'Afastado'),
  terminated('Terminated', 'Desligado');

  const EmployeeProfileStatus(this.code, this.label);

  final String code;
  final String label;

  static EmployeeProfileStatus fromCode(String code) => values.firstWhere(
    (status) => status.code == code,
    orElse: () => EmployeeProfileStatus.active,
  );
}
