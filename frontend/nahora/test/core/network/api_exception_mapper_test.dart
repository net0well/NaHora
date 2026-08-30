import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:nahora/core/errors/app_exception.dart';
import 'package:nahora/core/network/api_exception_mapper.dart';

/// The boundary where transport failures stop being technical.
///
/// Everything above this line works with a stable code; nothing above it should
/// ever see a status code or a socket error.
void main() {
  group('ApiExceptionMapper', () {
    DioException withResponse(int statusCode, Object? body) => DioException(
      requestOptions: RequestOptions(path: '/api/v1/punches'),
      response: Response<dynamic>(
        requestOptions: RequestOptions(path: '/api/v1/punches'),
        statusCode: statusCode,
        data: body,
      ),
      type: DioExceptionType.badResponse,
    );

    test(
      'Given a connection failure, When mapped, Then it is a network error',
      () {
        // Arrange & Act
        final mapped = ApiExceptionMapper.map(
          DioException(
            requestOptions: RequestOptions(path: '/'),
            type: DioExceptionType.connectionError,
          ),
        );

        // Assert
        expect(mapped, isA<NetworkException>());
      },
    );

    test('Given a 401, When mapped, Then it is unauthorized', () {
      // Arrange & Act
      final mapped = ApiExceptionMapper.map(
        withResponse(401, {'code': 'UNAUTHORIZED'}),
      );

      // Assert
      expect(mapped, isA<UnauthorizedException>());
    });

    test(
      'Given a 422 with field errors, When mapped, Then the fields survive',
      () {
        // Arrange — the form needs to know which field to mark, and Dio would
        // have discarded the body if the client were configured to swallow
        // error statuses
        final mapped = ApiExceptionMapper.map(
          withResponse(422, {
            'code': 'LOCATION_REQUIRED',
            'errors': {
              'location': ['A localização é obrigatória.'],
            },
          }),
        );

        // Assert
        expect(mapped, isA<ValidationException>());
        expect(mapped.code, 'LOCATION_REQUIRED');
        expect(
          (mapped as ValidationException).fieldErrors['location'],
          hasLength(1),
        );
      },
    );

    test(
      'Given a business failure, When mapped, Then the stable code survives',
      () {
        // Arrange & Act
        final mapped = ApiExceptionMapper.map(
          withResponse(403, {'code': 'EMPLOYEE_RECORD_REQUIRED'}),
        );

        // Assert
        expect(mapped, isA<BusinessException>());
        expect(mapped.code, 'EMPLOYEE_RECORD_REQUIRED');
      },
    );

    test(
      'Given a body with no code, When mapped, Then nothing technical escapes',
      () {
        // Arrange — an HTML error page from a proxy, for instance. It must not
        // become a message on screen.
        final mapped = ApiExceptionMapper.map(
          withResponse(500, '<html>oops</html>'),
        );

        // Assert
        expect(mapped, isA<UnexpectedException>());
        expect(ErrorMessages.resolve(mapped.code), isNot(contains('html')));
      },
    );
  });
}
