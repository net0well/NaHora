import 'package:dio/dio.dart';

import '../../../core/errors/app_exception.dart';
import '../../../core/network/api_exception_mapper.dart';
import '../domain/captured_location.dart';
import '../domain/punch_day.dart';
import '../domain/punch_direction.dart';
import '../domain/punch_history_page.dart';
import '../domain/punch_location.dart';
import '../domain/punch_receipt.dart';
import '../domain/work_day.dart';

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
    OfflineEstimate? offline,
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
          if (offline != null)
            'offline': {
              'occurredAtEstimate': offline.occurredAt
                  .toUtc()
                  .toIso8601String(),
              'monotonicSinceBootMs': offline.monotonicElapsedMs,
              'anchorAt': offline.anchorAt.toUtc().toIso8601String(),
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

  /// The hours the signed-in person is expected to work.
  ///
  /// The employee's own when they have any, the company's otherwise. Shown
  /// above the punch button so somebody recording a punch can see what the day
  /// was supposed to be — it never stops them recording it.
  Future<WorkSchedule> workSchedule() async {
    try {
      final response = await _client.get<Map<String, dynamic>>(
        '/api/v1/schedules/me',
      );
      final body = response.data;

      if (body == null) {
        throw const UnexpectedException();
      }

      return WorkSchedule.fromJson(body);
    } on DioException catch (exception) {
      throw ApiExceptionMapper.map(exception);
    }
  }

  /// Where one mark was made.
  ///
  /// One at a time, because that is what the server answers. The listings carry
  /// no coordinates on purpose, so this is the only way the app ever learns a
  /// position it did not itself just capture.
  Future<PunchLocation> punchLocation(String punchId) async {
    try {
      final response = await _client.get<Map<String, dynamic>>(
        '/api/v1/punches/$punchId/location',
      );

      final body = response.data;
      if (body == null) {
        throw const UnexpectedException();
      }

      return PunchLocation.fromJson(body);
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

/// The time a queued punch claims, and how the app arrived at it.
///
/// Sent only for a punch that waited on the device. Online the server stamps
/// the time and the client has no say.
class OfflineEstimate {
  const OfflineEstimate({
    required this.occurredAt,
    required this.monotonicElapsedMs,
    required this.anchorAt,
  });

  final DateTime occurredAt;

  /// The monotonic reading when the punch was made, so the estimate can be
  /// audited rather than merely believed.
  final int monotonicElapsedMs;

  /// The server instant the estimate counts from.
  final DateTime anchorAt;
}
