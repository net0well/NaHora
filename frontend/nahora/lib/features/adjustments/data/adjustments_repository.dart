import 'package:dio/dio.dart';

import '../../../core/errors/app_exception.dart';
import '../../../core/network/api_exception_mapper.dart';
import '../domain/adjustment.dart';
import '../domain/justification.dart';

/// Talks to the adjustment endpoints.
// Named parameters keep the call site readable while the field stays private.
// An initializing formal would expose `_client` as the parameter name.
// ignore_for_file: prefer_initializing_formals
class AdjustmentsRepository {
  AdjustmentsRepository({required Dio client}) : _client = client;

  final Dio _client;

  /// The corrections the signed-in person is entitled to see.
  ///
  /// Who that is comes from the token, not from a parameter: a manager gets the
  /// company's, anybody else gets their own. [pendingOnly] narrows what is
  /// returned — it never widens it.
  Future<List<Adjustment>> list({bool pendingOnly = false}) async {
    try {
      final response = await _client.get<List<dynamic>>(
        '/api/v1/adjustments',
        queryParameters: {'pendingOnly': pendingOnly},
      );

      final body = response.data;
      if (body == null) {
        throw const UnexpectedException();
      }

      return body
          .map((entry) => Adjustment.fromJson(entry as Map<String, dynamic>))
          .toList();
    } on DioException catch (exception) {
      throw ApiExceptionMapper.map(exception);
    }
  }

  /// The reasons a correction may be asked for.
  ///
  /// Fetched rather than hard-coded: the catalogue is the server's, and a copy
  /// in the app would drift from the one the audit reads.
  Future<List<Justification>> justifications() async {
    try {
      final response = await _client.get<List<dynamic>>(
        '/api/v1/justifications',
      );

      final body = response.data;
      if (body == null) {
        throw const UnexpectedException();
      }

      return body
          .map((entry) => Justification.fromJson(entry as Map<String, dynamic>))
          .toList()
        ..sort((a, b) => a.displayOrder.compareTo(b.displayOrder));
    } on DioException catch (exception) {
      throw ApiExceptionMapper.map(exception);
    }
  }

  /// Asks for a correction.
  ///
  /// With [punchId] it corrects an existing mark; without one it asks for a
  /// mark that was never made to count, and then [direction] says which
  /// boundary of the day it is.
  Future<Adjustment> request({
    required String justificationId,
    required DateTime occurredAt,
    String? punchId,
    String? direction,
    String? notes,
  }) async {
    try {
      final response = await _client.post<Map<String, dynamic>>(
        '/api/v1/adjustments',
        data: {
          'punchId': punchId,
          'occurredAt': occurredAt.toUtc().toIso8601String(),
          'direction': direction,
          'justificationId': justificationId,
          'notes': notes,
        },
      );

      final body = response.data;
      if (body == null) {
        throw const UnexpectedException();
      }

      return Adjustment.fromJson(body);
    } on DioException catch (exception) {
      throw ApiExceptionMapper.map(exception);
    }
  }

  /// Attaches the document that backs a request.
  ///
  /// A separate call on purpose, mirroring the server: the request is the
  /// record with legal weight and must survive an upload that fails on one bar
  /// of signal.
  Future<void> attach({
    required String adjustmentId,
    required String filePath,
  }) async {
    try {
      await _client.post<Map<String, dynamic>>(
        '/api/v1/adjustments/$adjustmentId/attachment',
        data: FormData.fromMap({
          'file': await MultipartFile.fromFile(filePath),
        }),
      );
    } on DioException catch (exception) {
      throw ApiExceptionMapper.map(exception);
    }
  }

  /// Approves or refuses one request.
  ///
  /// Only from here does the corrected time start to count. A refusal is kept
  /// rather than deleted: erasing it would hide that somebody asked.
  Future<Adjustment> decide({
    required String adjustmentId,
    required bool approve,
    String? notes,
  }) async {
    try {
      final response = await _client.post<Map<String, dynamic>>(
        '/api/v1/adjustments/$adjustmentId/decision',
        data: {'approve': approve, 'notes': notes},
      );

      final body = response.data;
      if (body == null) {
        throw const UnexpectedException();
      }

      return Adjustment.fromJson(body);
    } on DioException catch (exception) {
      throw ApiExceptionMapper.map(exception);
    }
  }
}
