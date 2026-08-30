import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:shadcn_ui/shadcn_ui.dart';

/// A labelled text input.
///
/// The error text is a parameter rather than a callback because validation
/// belongs to the controller. A field that decides on its own whether its
/// contents are valid duplicates a rule the server also has, and the two drift.
class AppTextField extends StatelessWidget {
  const AppTextField({
    required this.label,
    super.key,
    this.controller,
    this.placeholder,
    this.description,
    this.errorText,
    this.obscureText = false,
    this.enabled = true,
    this.keyboardType,
    this.inputFormatters,
    this.onSubmitted,
    this.focusNode,
  });

  final String label;
  final TextEditingController? controller;
  final String? placeholder;
  final String? description;
  final String? errorText;
  final bool obscureText;
  final bool enabled;
  final TextInputType? keyboardType;
  final List<TextInputFormatter>? inputFormatters;
  final ValueChanged<String>? onSubmitted;
  final FocusNode? focusNode;

  @override
  Widget build(BuildContext context) {
    return ShadInputFormField(
      label: Text(label),
      placeholder: placeholder == null ? null : Text(placeholder!),
      description: description == null ? null : Text(description!),

      // Shown as given. An empty string would render an empty error row and
      // shift the layout for no reason.
      forceErrorText: errorText,
      controller: controller,
      obscureText: obscureText,
      enabled: enabled,
      keyboardType: keyboardType,
      inputFormatters: inputFormatters,
      onSubmitted: onSubmitted,
      focusNode: focusNode,
    );
  }
}
