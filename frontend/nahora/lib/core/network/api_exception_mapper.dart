import 'package:dio/dio.dart';

import '../errors/app_exception.dart';

/// Turns a transport failure into something the UI can reason about.
///
/// Everything below this line is technical: status codes, socket errors, JSON
/// shapes. Everything above it works with an [AppException] carrying a stable
/// code. Keeping the translation in one place is what stops a `DioException`
/// from surfacing in a widget.
abstract final class ApiExceptionMapper {
  static AppException map(DioException exception) {
    switch (exception.type) {
      case DioExceptionType.connectionTimeout:
      case DioExceptionType.sendTimeout:
      case DioExceptionType.receiveTimeout:
      case DioExceptionType.connectionError:
        return const NetworkException();

      case DioExceptionType.cancel:
        return const NetworkException();

      case DioExceptionType.badCertificate:
        // A certificate the device refuses is not a network hiccup; it is the
        // case HTTPS exists to catch. Reported as a network failure to the
        // user, who can do nothing about it either way.
        return const NetworkException();

      case DioExceptionType.badResponse:
      case DioExceptionType.unknown:
      // Any type Dio adds later lands here rather than failing to compile at
      // the worst moment. An unclassified transport failure is a network
      // failure as far as the screen is concerned.
      // ignore: unreachable_switch_default
      default:
        return _fromResponse(exception.response);
    }
  }

  static AppException _fromResponse(Response<dynamic>? response) {
    if (response == null) {
      return const NetworkException();
    }

    if (response.statusCode == 401) {
      return const UnauthorizedException();
    }

    final body = response.data;
    if (body is! Map<String, dynamic>) {
      return const UnexpectedException();
    }

    // The API answers RFC 9457 problem+json and carries a stable `code`. That
    // code is the contract; `title` and `detail` are free to be rewritten.
    final code = body['code'];
    if (code is! String || code.isEmpty) {
      return const UnexpectedException();
    }

    if (response.statusCode == 422) {
      return ValidationException(code, _readFieldErrors(body));
    }

    return BusinessException(code);
  }

  static Map<String, List<String>> _readFieldErrors(Map<String, dynamic> body) {
    final errors = body['errors'];
    if (errors is! Map<String, dynamic>) {
      return const {};
    }

    return errors.map(
      (field, messages) => MapEntry(
        field,
        messages is List
            ? messages.map((message) => message.toString()).toList()
            : <String>[],
      ),
    );
  }
}
