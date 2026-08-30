import 'package:nahora/core/errors/app_exception.dart';
import 'package:nahora/features/attendance/data/attendance_repository.dart';
import 'package:nahora/features/attendance/domain/captured_location.dart';
import 'package:nahora/features/attendance/domain/punch_day.dart';
import 'package:nahora/features/attendance/domain/punch_direction.dart';
import 'package:nahora/features/attendance/domain/punch_entry.dart';
import 'package:nahora/features/attendance/domain/punch_history_page.dart';
import 'package:nahora/features/attendance/domain/punch_receipt.dart';
import 'package:nahora/features/attendance/domain/punch_trust.dart';
import 'package:nahora/features/attendance/domain/work_day.dart';

/// A stand-in for the real repository, shared by every test that needs one.
///
/// A fake rather than a mock: these tests assert what the controller and the
/// screens do with the answer, never how many times they asked.
class FakeAttendanceRepository implements AttendanceRepository {
  FakeAttendanceRepository({PunchDay? day, PunchHistoryPage? historyPage})
    : day = day ?? emptyDay(),
      historyPage = historyPage ?? emptyHistory();

  static final DateTime serverStamp = DateTime.utc(2026, 8, 30, 17, 32);

  /// São Paulo. Not zero, so a screen that forgets to apply the offset shows a
  /// visibly wrong time instead of passing by accident.
  static const Duration companyOffset = Duration(hours: -3);

  PunchDay day;

  /// Named apart from the [history] method: a field and a method cannot
  /// share a name.
  PunchHistoryPage historyPage;

  WorkSchedule schedule = defaultSchedule();

  final List<CapturedLocation> sentLocations = [];
  final List<DateTime> sentDeviceClocks = [];

  /// Thrown by [registerPunch] when set.
  AppException? failure;

  /// Thrown by [punchesForDay] and [history] when set.
  AppException? readFailure;

  @override
  Future<ServerTime> serverTime() async =>
      ServerTime(utc: serverStamp, utcOffset: companyOffset);

  @override
  Future<PunchReceipt> registerPunch({
    required String clientId,
    required CapturedLocation location,
    required String deviceId,
    required DateTime deviceClockAt,
  }) async {
    if (failure != null) {
      throw failure!;
    }

    sentLocations.add(location);
    sentDeviceClocks.add(deviceClockAt);

    return PunchReceipt(
      id: 'punch-1',
      nsr: 1,
      direction: PunchDirection.entry,
      occurredAt: serverStamp,
      trust: 'Trusted',
      alreadyRegistered: false,
    );
  }

  @override
  Future<WorkSchedule> workSchedule() async {
    if (readFailure != null) {
      throw readFailure!;
    }

    return schedule;
  }

  @override
  Future<PunchDay> punchesForDay({String? localDate}) async {
    if (readFailure != null) {
      throw readFailure!;
    }

    return day;
  }

  @override
  Future<PunchHistoryPage> history({int page = 1, int pageSize = 30}) async {
    if (readFailure != null) {
      throw readFailure!;
    }

    return historyPage;
  }

  /// Monday to Friday, eight to five with an hour off.
  static WorkSchedule defaultSchedule({bool isWorkday = true}) {
    const day = WorkDay(
      dayOfWeek: 1,
      isWorkday: true,
      expectedMinutes: 480,
      expectedPunches: 4,
      describe: '08:00 às 17:00, com 1h de intervalo',
      startTime: '08:00',
      lunchStartTime: '12:00',
      lunchEndTime: '13:00',
      endTime: '17:00',
    );

    const off = WorkDay(
      dayOfWeek: 0,
      isWorkday: false,
      expectedMinutes: 0,
      expectedPunches: 0,
      describe: 'Sem expediente',
    );

    return WorkSchedule(
      source: WorkScheduleSource.company,
      timeZoneId: 'America/Sao_Paulo',
      localDate: '2026-08-31',
      today: isWorkday ? day : off,
      week: const [off, day, day, day, day, day, off],
    );
  }

  static PunchDay emptyDay() => const PunchDay(
    localDate: '2026-08-30',
    timeZoneId: 'America/Sao_Paulo',
    nextDirection: PunchDirection.entry,
    entries: [],
  );

  static PunchHistoryPage emptyHistory() => const PunchHistoryPage(
    page: 1,
    pageSize: 30,
    totalCount: 0,
    hasMore: false,
    entries: [],
  );

  /// One recorded punch, with the pieces a screen actually renders.
  static PunchEntry entry({
    int nsr = 1,
    PunchDirection direction = PunchDirection.entry,
    PunchTrust trust = PunchTrust.trusted,
    DateTime? occurredAt,
    bool isOffline = false,
    String localDate = '2026-08-30',
  }) => PunchEntry(
    id: 'punch-$nsr',
    nsr: nsr,
    direction: direction,
    trust: trust,
    occurredAt: occurredAt ?? DateTime.utc(2026, 8, 30, 11, 5),
    companyOffset: companyOffset,
    accuracyMeters: 12,
    isOffline: isOffline,
    localDate: localDate,
  );
}
