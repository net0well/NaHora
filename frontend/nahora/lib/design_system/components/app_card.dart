import 'package:flutter/material.dart';
import 'package:shadcn_ui/shadcn_ui.dart';

import '../theme/app_spacing.dart';

/// A bounded region of related content.
class AppCard extends StatelessWidget {
  const AppCard({
    required this.child,
    super.key,
    this.title,
    this.description,
    this.footer,
    this.padding,
  });

  final Widget child;
  final String? title;
  final String? description;
  final Widget? footer;
  final EdgeInsets? padding;

  @override
  Widget build(BuildContext context) {
    return ShadCard(
      title: title == null ? null : Text(title!),
      description: description == null ? null : Text(description!),
      footer: footer,
      padding: padding ?? const EdgeInsets.all(AppSpacing.lg),
      child: child,
    );
  }
}
