import 'package:dio/dio.dart';

import '../../../core/errors/app_exception.dart';
import '../../../core/network/api_exception_mapper.dart';
import '../domain/company_profile.dart';
import '../domain/employee_profile.dart';

/// Talks to the endpoints that describe who the caller is and who they work
/// for.
// Named parameters keep the call site readable while the field stays private.
// An initializing formal would expose `_client` as the parameter name.
// ignore_for_file: prefer_initializing_formals
class ProfileRepository {
  ProfileRepository({required Dio client}) : _client = client;

  final Dio _client;

  /// The caller's own employment record.
  Future<EmployeeProfile> employee() async {
    final body = await _get('/api/v1/employees/me');

    return EmployeeProfile.fromJson(body);
  }

  /// The company the caller works for.
  Future<CompanyProfile> company() async {
    final body = await _get('/api/v1/companies/me');

    return CompanyProfile.fromJson(body);
  }

  Future<Map<String, dynamic>> _get(String path) async {
    try {
      final response = await _client.get<Map<String, dynamic>>(path);
      final body = response.data;

      if (body == null) {
        throw const UnexpectedException();
      }

      return body;
    } on DioException catch (exception) {
      throw ApiExceptionMapper.map(exception);
    }
  }
}
