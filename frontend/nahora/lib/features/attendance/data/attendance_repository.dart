import 'package:dio/dio.dart';

import '../../../core/errors/app_exception.dart';
import '../../../core/network/api_exception_mapper.dart';
import '../domain/captured_location.dart';
import '../domain/punch_day.dart';
import '../domain/punch_direction.dart';
import '../domain/punch_history_page.dart';
import '../domain/punch_receipt.dart';

/// Talks to the attendance endpoints.
// Named parameters keep the call site readable while the field stays private.
// An initializing formal would expose `_client` as the parameter name.
// ignore_for_file: prefer_initializing_formals
class AttendanceRepository {
  AttendanceRepository({required Dio client}) : _client = client;

  final Dio _client;

  /// The official time, for anchoring the clock.
  Future<ServerTime> serverTime() async {
    try {
      final response = await _client.get<Map<String, dynamic>>('/api/v1/time');
      final body = response.data;

      if (body == null) {
        throw const UnexpectedException();
      }

      return ServerTime(
        utc: DateTime.parse(body['serverTimeUtc'] as String).toUtc(),
        utcOffset: Duration(seconds: (body['utcOffsetSeconds'] as int?) ?? 0),
      );
    } on DioException catch (exception) {
      throw ApiExceptionMapper.map(exception);
    }
  }

  /// Records a punch.
  ///
  /// The time is absent on purpose. A client that could send it would let an
  /// employee punch early by changing the phone clock, so the server stamps it.
  /// [deviceClockAt] travels only as evidence of how far off the device was.
  Future<PunchReceipt> registerPunch({
    required String clientId,
    required CapturedLocation location,
    required String deviceId,
    required DateTime deviceClockAt,
  }) async {
    try {
      final response = await _client.post<Map<String, dynamic>>(
        '/api/v1/punches',
        data: {
          'clientId': clientId,
          'location': {
            'latitude': location.latitude,
            'longitude': location.longitude,
            'accuracyMeters': location.accuracyMeters,
            'capturedAt': location.capturedAt.toUtc().toIso8601String(),
          },
          'device': {
            'deviceId': deviceId,
            'locationWasMocked': location.isMocked,
            'deviceClockAt': deviceClockAt.toUtc().toIso8601String(),
          },
        },
      );

      final body = response.data;
      if (body == null) {
        throw const UnexpectedException();
      }

      return PunchReceipt(
        id: body['id'] as String,
        nsr: body['nsr'] as int,
        direction: PunchDirection.fromCode(body['direction'] as String),
        occurredAt: DateTime.parse(body['occurredAt'] as String).toUtc(),
        trust: body['trust'] as String,
        alreadyRegistered: (body['alreadyRegistered'] as bool?) ?? false,
      );
    } on DioException catch (exception) {
      throw ApiExceptionMapper.map(exception);
    }
  }

  /// The employee's own punches for one working day.
  ///
  /// Sends no date by default. The day is resolved on the server in company
  /// time, because a phone whose date is a day out would otherwise ask for a
  /// day that never happened and be told, correctly, that it is empty.
  Future<PunchDay> punchesForDay({String? localDate}) async {
    try {
      final response = await _client.get<Map<String, dynamic>>(
        '/api/v1/punches/me',
        queryParameters: localDate == null ? null : {'localDate': localDate},
      );

      final body = response.data;
      if (body == null) {
        throw const UnexpectedException();
      }

      return PunchDay.fromJson(body);
    } on DioException catch (exception) {
      throw ApiExceptionMapper.map(exception);
    }
  }

  /// A page of the employee's own history, newest first.
  Future<PunchHistoryPage> history({int page = 1, int pageSize = 30}) async {
    try {
      final response = await _client.get<Map<String, dynamic>>(
        '/api/v1/punches/me/history',
        queryParameters: {'page': page, 'pageSize': pageSize},
      );

      final body = response.data;
      if (body == null) {
        throw const UnexpectedException();
      }

      return PunchHistoryPage.fromJson(body);
    } on DioException catch (exception) {
      throw ApiExceptionMapper.map(exception);
    }
  }
}

/// The server's clock and the company's offset.
class ServerTime {
  const ServerTime({required this.utc, required this.utcOffset});

  final DateTime utc;

  /// The company's offset, sent by the server rather than derived on the
  /// device: a phone with an outdated timezone database would compute a
  /// different one after a daylight-saving change.
  final Duration utcOffset;
}
