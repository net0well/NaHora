import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';

/// A scripted HTTP layer, so a test can drive Dio without a server.
///
/// Records every request it answered, which is what lets a test assert that a
/// retry happened exactly once rather than inferring it from the final result.
class FakeHttpAdapter implements HttpClientAdapter {
  FakeHttpAdapter(this._respond);

  final ResponseBody Function(RequestOptions options, int callNumber) _respond;

  final List<RequestOptions> requests = [];

  int get callCount => requests.length;

  /// How many times a given path was asked for.
  int callsTo(String path) =>
      requests.where((request) => request.path == path).length;

  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) async {
    requests.add(options);

    return _respond(options, requests.length);
  }

  @override
  void close({bool force = false}) {}
}

/// Builds a JSON response body for the adapter.
ResponseBody jsonResponse(int statusCode, Map<String, dynamic> body) =>
    ResponseBody.fromString(
      jsonEncode(body),
      statusCode,
      headers: {
        Headers.contentTypeHeader: [Headers.jsonContentType],
      },
    );
