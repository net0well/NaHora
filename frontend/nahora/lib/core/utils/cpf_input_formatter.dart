import 'package:flutter/services.dart';

/// Formats a CPF as it is typed: 000.000.000-00.
///
/// Formatting only. Validity is the server's answer — a check digit verified
/// here would be a rule living in two places, and the two drift.
class CpfInputFormatter extends TextInputFormatter {
  static const int _length = 11;

  /// Strips the mask, which is what the API expects.
  static String digitsOf(String value) => value.replaceAll(RegExp(r'\D'), '');

  @override
  TextEditingValue formatEditUpdate(
    TextEditingValue oldValue,
    TextEditingValue newValue,
  ) {
    final digits = digitsOf(newValue.text);
    if (digits.length > _length) {
      return oldValue;
    }

    final buffer = StringBuffer();
    for (var i = 0; i < digits.length; i++) {
      if (i == 3 || i == 6) {
        buffer.write('.');
      } else if (i == 9) {
        buffer.write('-');
      }
      buffer.write(digits[i]);
    }

    final text = buffer.toString();

    // The caret goes to the end. Anything cleverer has to track separators the
    // formatter itself inserted, and gets it wrong on backspace.
    return TextEditingValue(
      text: text,
      selection: TextSelection.collapsed(offset: text.length),
    );
  }
}
