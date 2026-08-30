/// The hours one day of the week is expected to run.
///
/// Information, never a gate. Portaria 671, Art. 78 §1º forbids the registrar
/// from restricting a punch, so nothing derived from this can stop one — it
/// exists so somebody about to record a punch can see what the day was supposed
/// to be.
class WorkDay {
  const WorkDay({
    required this.dayOfWeek,
    required this.isWorkday,
    required this.expectedMinutes,
    required this.expectedPunches,
    required this.describe,
    this.startTime,
    this.lunchStartTime,
    this.lunchEndTime,
    this.endTime,
  });

  factory WorkDay.fromJson(Map<String, dynamic> json) => WorkDay(
    dayOfWeek: json['dayOfWeekNumber'] as int,
    isWorkday: json['isWorkday'] as bool,
    expectedMinutes: json['expectedMinutes'] as int,
    expectedPunches: json['expectedPunches'] as int,
    describe: json['describe'] as String,
    startTime: json['startTime'] as String?,
    lunchStartTime: json['lunchStartTime'] as String?,
    lunchEndTime: json['lunchEndTime'] as String?,
    endTime: json['endTime'] as String?,
  );

  /// Sunday is 0, matching both the API and Dart's own weekday numbering once
  /// Sunday's 7 is folded to 0.
  final int dayOfWeek;

  final bool isWorkday;
  final int expectedMinutes;

  /// What a complete day looks like — four with a break, two without.
  final int expectedPunches;

  /// Already assembled by the server: "08:00 às 17:00, com 1h de intervalo".
  final String describe;

  final String? startTime;
  final String? lunchStartTime;
  final String? lunchEndTime;
  final String? endTime;
}

/// The week that governs somebody, and which schedule it came from.
class WorkSchedule {
  const WorkSchedule({
    required this.source,
    required this.timeZoneId,
    required this.localDate,
    required this.today,
    required this.week,
  });

  factory WorkSchedule.fromJson(Map<String, dynamic> json) => WorkSchedule(
    source: WorkScheduleSource.fromCode(json['source'] as String),
    timeZoneId: json['timeZoneId'] as String,
    localDate: json['localDate'] as String,
    today: WorkDay.fromJson(json['today'] as Map<String, dynamic>),
    week: [
      for (final day in (json['week'] as List<dynamic>? ?? const []))
        WorkDay.fromJson(day as Map<String, dynamic>),
    ],
  );

  final WorkScheduleSource source;
  final String timeZoneId;

  /// The working day in company time, decided on the server.
  final String localDate;

  final WorkDay today;
  final List<WorkDay> week;
}

/// Whose hours these are.
enum WorkScheduleSource {
  employee('Employee', 'Sua jornada'),
  company('Company', 'Jornada da empresa'),
  none('None', 'Jornada não definida');

  const WorkScheduleSource(this.code, this.label);

  final String code;
  final String label;

  static WorkScheduleSource fromCode(String code) => values.firstWhere(
    (source) => source.code == code,
    orElse: () => WorkScheduleSource.none,
  );
}
