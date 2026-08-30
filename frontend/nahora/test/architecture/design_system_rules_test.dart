import 'dart:io';

import 'package:flutter_test/flutter_test.dart';

/// Rules that hold the design system together, checked against the source.
///
/// These are the lint rules spec 010 asks for (AC-010-1 and AC-010-2), written
/// as tests rather than as a custom analyzer plugin: they need no extra
/// dependency, they run with `flutter test`, and a violation fails the build
/// the same way.
///
/// What they protect is not tidiness. A literal colour in a feature widget is
/// invisible until someone restyles the product and finds three screens that
/// did not change; an import of the network layer inside a widget is invisible
/// until someone tries to test that widget without a server.
void main() {
  group('Design system rules', () {
    late List<File> featureSources;

    setUp(() {
      featureSources = _dartFilesUnder('lib/features');
    });

    test('Given a feature widget, When its source is read, '
        'Then it declares no literal colour', () {
      // Arrange
      final offenders = <String>[];

      // Act
      for (final file in featureSources) {
        final source = file.readAsStringSync();

        for (final match in _colourLiteral.allMatches(source)) {
          offenders.add('${file.path}: ${match.group(0)}');
        }
      }

      // Assert
      expect(
        offenders,
        isEmpty,
        reason:
            'colour belongs to AppColors, never to a screen:\n${offenders.join('\n')}',
      );
    });

    test('Given a feature widget, When its source is read, '
        'Then it declares no literal spacing or radius', () {
      // Arrange
      final offenders = <String>[];

      // Act
      for (final file in featureSources) {
        final source = file.readAsStringSync();

        for (final pattern in [_edgeInsetsLiteral, _radiusLiteral]) {
          for (final match in pattern.allMatches(source)) {
            // A token reference carries no digits, so any digit in the
            // arguments is a value invented at the call site.
            if (_containsDigit.hasMatch(match.group(1) ?? '')) {
              offenders.add('${file.path}: ${match.group(0)}');
            }
          }
        }
      }

      // Assert
      expect(
        offenders,
        isEmpty,
        reason:
            'spacing comes from AppSpacing and radius from AppRadius:\n${offenders.join('\n')}',
      );
    });

    test('Given a widget outside the data layer, When its imports are read, '
        'Then none reaches the network layer directly', () {
      // Arrange — the data layer is the only place allowed to know the
      // transport. A widget that calls the API cannot be rendered in a test
      // without one, and the controller it skipped is where the state lives.
      final offenders = <String>[];

      // Act
      for (final file in featureSources) {
        if (_isDataLayer(file)) {
          continue;
        }

        if (_networkImport.hasMatch(file.readAsStringSync())) {
          offenders.add(file.path);
        }
      }

      // Assert
      expect(
        offenders,
        isEmpty,
        reason:
            'a widget talks to a controller, never to ApiClient:\n${offenders.join('\n')}',
      );
    });

    test('Given the rules themselves, When they run, '
        'Then they were pointed at files that exist', () {
      // A rule that silently scans nothing passes forever. This is the guard
      // against the whole suite above becoming decorative after a move.
      expect(featureSources, isNotEmpty);
    });

    group('The detectors themselves', () {
      // A rule that passes because its pattern never matches anything is worse
      // than no rule: it reports success. These pin the patterns against
      // violations they must catch, and against valid code they must not.

      test(
        'Given a literal colour, When the pattern runs, Then it is caught',
        () {
          expect(_colourLiteral.hasMatch('color: Color(0xFF1B5FA8)'), isTrue);
          expect(_colourLiteral.hasMatch('color: Colors.red'), isTrue);
          expect(_colourLiteral.hasMatch('color: AppColors.primary'), isFalse);
        },
      );

      test(
        'Given literal spacing, When the pattern runs, Then it is caught',
        () {
          final literal = _edgeInsetsLiteral.firstMatch('EdgeInsets.all(12)');
          expect(_containsDigit.hasMatch(literal!.group(1)!), isTrue);

          final token = _edgeInsetsLiteral.firstMatch(
            'EdgeInsets.symmetric(horizontal: AppSpacing.md)',
          );
          expect(_containsDigit.hasMatch(token!.group(1)!), isFalse);
        },
      );

      test(
        'Given a literal radius, When the pattern runs, Then it is caught',
        () {
          final literal = _radiusLiteral.firstMatch('BorderRadius.circular(8)');
          expect(_containsDigit.hasMatch(literal!.group(1)!), isTrue);

          final token = _radiusLiteral.firstMatch(
            'BorderRadius.circular(AppRadius.sm)',
          );
          expect(_containsDigit.hasMatch(token!.group(1)!), isFalse);
        },
      );

      test(
        'Given a network import, When the pattern runs, Then it is caught',
        () {
          expect(
            _networkImport.hasMatch(
              "import '../../../core/network/api_client.dart';",
            ),
            isTrue,
          );
          expect(
            _networkImport.hasMatch(
              "import '../../../core/storage/token.dart';",
            ),
            isFalse,
          );
        },
      );
    });
  });
}

/// `Color(0xFF...)` and Material's named palette, both of which bypass tokens.
final _colourLiteral = RegExp(r'\bColor\(0x[0-9a-fA-F]+\)|\bColors\.[a-zA-Z]+');

final _edgeInsetsLiteral = RegExp(r'\bEdgeInsets\.\w+\(([^()]*)\)');

final _radiusLiteral = RegExp(r'\bBorderRadius\.circular\(([^()]*)\)');

final _containsDigit = RegExp(r'\d');

final _networkImport = RegExp(
  r"""import\s+['"][^'"]*core/network/(api_client|auth_interceptor|api_exception_mapper)\.dart['"]""",
);

bool _isDataLayer(File file) =>
    file.path.replaceAll(r'\', '/').contains('/data/');

List<File> _dartFilesUnder(String path) =>
    Directory(path)
        .listSync(recursive: true)
        .whereType<File>()
        .where((file) => file.path.endsWith('.dart'))
        .toList();
