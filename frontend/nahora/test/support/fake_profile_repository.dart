import 'package:nahora/core/errors/app_exception.dart';
import 'package:nahora/features/profile/data/profile_repository.dart';
import 'package:nahora/features/profile/domain/company_profile.dart';
import 'package:nahora/features/profile/domain/employee_profile.dart';
import 'package:nahora/features/profile/domain/postal_address.dart';

/// A stand-in for the profile endpoints.
class FakeProfileRepository implements ProfileRepository {
  FakeProfileRepository({EmployeeProfile? employee, CompanyProfile? company})
    : employeeProfile = employee ?? defaultEmployee(),
      companyProfile = company ?? defaultCompany();

  /// Named apart from the methods below: a field and a method cannot share a
  /// name.
  EmployeeProfile employeeProfile;
  CompanyProfile companyProfile;

  /// Thrown by both reads when set.
  AppException? failure;

  @override
  Future<EmployeeProfile> employee() async {
    if (failure != null) {
      throw failure!;
    }

    return employeeProfile;
  }

  @override
  Future<CompanyProfile> company() async {
    if (failure != null) {
      throw failure!;
    }

    return companyProfile;
  }

  static PostalAddress address() => const PostalAddress(
    postalCode: '01310-100',
    street: 'Avenida Paulista',
    number: '1000',
    district: 'Bela Vista',
    city: 'São Paulo',
    state: 'SP',
    singleLine:
        'Avenida Paulista, 1000 - Bela Vista - São Paulo/SP - 01310-100',
  );

  static EmployeeProfile defaultEmployee({
    String? pis = '123.45678.90-0',
    bool isExportableToAfd = true,
  }) => EmployeeProfile(
    id: 'employee-1',
    fullName: 'Joana Funcionária',
    cpf: '529.982.247-25',
    pis: pis,
    registration: '0001',
    position: 'Operadora',
    department: 'Operações',
    contractType: EmployeeContractType.clt,
    status: EmployeeProfileStatus.active,
    hiredAt: '2026-01-15',
    birthDate: '1995-04-12',
    email: 'funcionaria@nahora.dev',
    phone: '(11) 98888-7777',
    address: address(),
    isExportableToAfd: isExportableToAfd,
  );

  static CompanyProfile defaultCompany({bool registryVerified = true}) =>
      CompanyProfile(
        id: 'company-1',
        cnpj: '11.222.333/0001-81',
        legalName: 'NaHora Desenvolvimento LTDA',
        tradeName: 'NaHora Dev',
        email: 'contato@nahora.dev',
        phone: '(11) 3333-4444',
        stateRegistration: 'ISENTO',
        timeZoneId: 'America/Sao_Paulo',
        status: 'Active',
        registryVerified: registryVerified,
        address: address(),
      );
}
