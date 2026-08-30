import 'punch_entry.dart';

/// One page of the employee's own punch history, newest first.
class PunchHistoryPage {
  const PunchHistoryPage({
    required this.page,
    required this.pageSize,
    required this.totalCount,
    required this.hasMore,
    required this.entries,
  });

  factory PunchHistoryPage.fromJson(Map<String, dynamic> json) =>
      PunchHistoryPage(
        page: json['page'] as int,
        pageSize: json['pageSize'] as int,
        totalCount: json['totalCount'] as int,
        hasMore: json['hasMore'] as bool? ?? false,
        entries: [
          for (final entry in (json['entries'] as List<dynamic>? ?? const []))
            PunchEntry.fromJson(entry as Map<String, dynamic>),
        ],
      );

  /// The page the server actually served, which may not be the one asked for.
  final int page;

  final int pageSize;

  /// Every punch the employee has, not just the ones on this page.
  final int totalCount;

  /// Whether another page exists.
  ///
  /// Taken from the server rather than derived here. Computing it from the
  /// other three gets the boundary wrong, and the symptom is a list that never
  /// stops loading.
  final bool hasMore;

  final List<PunchEntry> entries;
}
