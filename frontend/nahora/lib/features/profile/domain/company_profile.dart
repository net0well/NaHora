import 'postal_address.dart';

/// The company the signed-in person works for.
class CompanyProfile {
  const CompanyProfile({
    required this.id,
    required this.cnpj,
    required this.legalName,
    required this.email,
    required this.timeZoneId,
    required this.status,
    required this.registryVerified,
    required this.address,
    this.tradeName,
    this.phone,
    this.stateRegistration,
    this.municipalRegistration,
    this.foundedOn,
  });

  factory CompanyProfile.fromJson(Map<String, dynamic> json) => CompanyProfile(
    id: json['id'] as String,

    // The punctuated form. Every screen shows it that way, and the server
    // already assembled it.
    cnpj: json['cnpjFormatted'] as String? ?? json['cnpj'] as String,
    legalName: json['legalName'] as String,
    tradeName: json['tradeName'] as String?,
    email: json['email'] as String,
    phone: json['phoneFormatted'] as String? ?? json['phone'] as String?,
    stateRegistration: json['stateRegistration'] as String?,
    municipalRegistration: json['municipalRegistration'] as String?,
    foundedOn: json['foundedOn'] as String?,
    timeZoneId: json['timeZoneId'] as String,
    status: json['status'] as String,
    registryVerified: json['registryVerified'] as bool? ?? false,
    address: PostalAddress.fromJson(json['address'] as Map<String, dynamic>),
  );

  final String id;
  final String cnpj;

  /// Razão social.
  final String legalName;

  /// Nome fantasia.
  final String? tradeName;

  final String email;
  final String? phone;

  /// Inscrição estadual, or null when exempt.
  final String? stateRegistration;

  final String? municipalRegistration;
  final String? foundedOn;
  final String timeZoneId;
  final String status;

  /// Whether the tax registry confirmed the CNPJ.
  ///
  /// False also covers "the registry could not be reached" — an outage leaves
  /// the confirmation outstanding rather than failing the company.
  final bool registryVerified;

  final PostalAddress address;

  /// What to call the company on screen.
  String get displayName => tradeName ?? legalName;
}
