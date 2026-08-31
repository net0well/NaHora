import 'dart:convert';

import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import '../domain/pending_punch.dart';
import '../domain/punch_queue.dart';

/// The queue, in the platform's encrypted store.
///
/// Not SharedPreferences and not a file: a queued punch carries a coordinate,
/// and a coordinate beside an employee says where that person was at that
/// minute. Plain SharedPreferences is an XML file any process running as the
/// user can read — OWASP Mobile M9. This is EncryptedSharedPreferences on
/// Android and the Keychain on iOS, the same store the session token lives in.
///
/// A handful of entries fits comfortably. It is a queue of punches somebody
/// made in a basement, not a database.
class SecureStoragePunchQueue implements PunchQueue {
  SecureStoragePunchQueue({FlutterSecureStorage? storage})
    : _storage =
          storage ??
          const FlutterSecureStorage(
            iOptions: IOSOptions(
              // This device only: a queue restored onto another handset would
              // send somebody else's marks from a phone they never held.
              accessibility: KeychainAccessibility.first_unlock_this_device,
            ),
          );

  static const String _key = 'nahora.punch_queue';

  /// Above this the oldest is dropped.
  ///
  /// A cap rather than none: a phone left offline for a month would otherwise
  /// grow a store nothing ever trims. Fifty is far more than a working week of
  /// marks, so reaching it means something else is wrong.
  static const int _capacity = 50;

  final FlutterSecureStorage _storage;

  @override
  Future<List<PendingPunch>> pending() async {
    final raw = await _storage.read(key: _key);
    if (raw == null || raw.isEmpty) {
      return [];
    }

    try {
      return (jsonDecode(raw) as List<dynamic>)
          .map((entry) => PendingPunch.fromJson(entry as Map<String, dynamic>))
          .toList();
    } on FormatException {
      // Unreadable rather than absent. Keeping it would fail every drain from
      // here on, and there is nothing in it anybody can act on.
      await clear();

      return [];
    }
  }

  @override
  Future<void> add(PendingPunch punch) async {
    final queue = await pending()
      ..add(punch);

    await _write(
      queue.length > _capacity
          ? queue.sublist(queue.length - _capacity)
          : queue,
    );
  }

  @override
  Future<void> remove(String clientId) async {
    final queue = await pending();

    await _write(queue.where((punch) => punch.clientId != clientId).toList());
  }

  @override
  Future<void> clear() => _storage.delete(key: _key);

  Future<void> _write(List<PendingPunch> queue) async {
    if (queue.isEmpty) {
      await clear();

      return;
    }

    await _storage.write(
      key: _key,
      value: jsonEncode([for (final punch in queue) punch.toJson()]),
    );
  }
}
