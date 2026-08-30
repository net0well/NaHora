/// A postal address, as the API sends it.
///
/// Not a punch location. This is where a company or a person is registered,
/// typed by a human — it carries no coordinates and never feeds the attendance
/// record.
class PostalAddress {
  const PostalAddress({
    required this.postalCode,
    required this.street,
    required this.number,
    required this.city,
    required this.state,
    required this.singleLine,
    this.complement,
    this.district,
  });

  factory PostalAddress.fromJson(Map<String, dynamic> json) => PostalAddress(
    postalCode: json['postalCode'] as String,
    street: json['street'] as String,
    number: json['number'] as String,
    complement: json['complement'] as String?,
    district: json['district'] as String?,
    city: json['city'] as String,
    state: json['state'] as String,
    singleLine: json['singleLine'] as String,
  );

  final String postalCode;
  final String street;
  final String number;
  final String? complement;
  final String? district;
  final String city;
  final String state;

  /// Assembled by the server, so a narrow screen renders one line without
  /// every client reinventing the punctuation.
  final String singleLine;
}
