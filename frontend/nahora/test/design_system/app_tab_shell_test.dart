import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:nahora/design_system/components/app_tab_shell.dart';
import 'package:nahora/design_system/theme/app_colors.dart';

import '../support/pump_design_system.dart';

/// The shell: two tabs with the punch action raised between them.
void main() {
  group('AppTabShell', () {
    testWidgets(
      'Given the shell, When rendered, Then both tabs and the action are present',
      (tester) async {
        // Arrange & Act
        await pumpComponent(
          tester,
          AppTabShell(
            currentTab: AppTab.employee,
            onTabSelected: (_) {},
            onPrimaryAction: () {},
            primaryTime: null,
            child: const SizedBox.shrink(),
          ),
        );

        // Assert
        expect(find.text('Funcionário'), findsOneWidget);
        expect(find.text('Histórico'), findsOneWidget);
        expect(find.bySemanticsLabel('Marcar ponto'), findsAtLeastNWidgets(1));
      },
    );

    testWidgets(
      'Given a tab is tapped, When it happens, Then the selection is reported',
      (tester) async {
        // Arrange
        AppTab? selected;
        await pumpComponent(
          tester,
          AppTabShell(
            currentTab: AppTab.employee,
            onTabSelected: (tab) => selected = tab,
            onPrimaryAction: () {},
            primaryTime: null,
            child: const SizedBox.shrink(),
          ),
        );

        // Act
        await tester.tap(find.text('Histórico'));
        await tester.pumpAndSettle();

        // Assert
        expect(selected, AppTab.history);
      },
    );

    testWidgets('Given the central action, When tapped, Then it fires', (
      tester,
    ) async {
      // Arrange
      var taps = 0;
      await pumpComponent(
        tester,
        AppTabShell(
          currentTab: AppTab.employee,
          onTabSelected: (_) {},
          onPrimaryAction: () => taps++,
          primaryTime: null,
          child: const SizedBox.shrink(),
        ),
      );

      // Act
      await tester.tap(find.byKey(const Key('punch_action_clock')));
      await tester.pumpAndSettle();

      // Assert
      expect(taps, 1);
    });

    testWidgets('Given the selected tab, When rendered, '
        'Then selection is carried by weight as well as colour', (
      tester,
    ) async {
      // Arrange — colour alone would tell a colour-blind user nothing about
      // which tab they are on
      await pumpComponent(
        tester,
        AppTabShell(
          currentTab: AppTab.history,
          onTabSelected: (_) {},
          onPrimaryAction: () {},
          primaryTime: null,
          child: const SizedBox.shrink(),
        ),
      );

      // Act
      final selected = tester.widget<Text>(find.text('Histórico'));
      final other = tester.widget<Text>(find.text('Funcionário'));

      // Assert
      expect(selected.style?.fontWeight, FontWeight.w700);
      expect(other.style?.fontWeight, FontWeight.w400);
      expect(selected.style?.color, AppColors.primary);
    });

    testWidgets(
      'Given no action, When rendered, Then the central button is disabled',
      (tester) async {
        // Arrange & Act
        await pumpComponent(
          tester,
          const AppTabShell(
            currentTab: AppTab.employee,
            onTabSelected: _ignore,
            onPrimaryAction: null,
            primaryTime: null,
            child: SizedBox.shrink(),
          ),
        );

        // Assert
        expect(
          tester.getSemantics(find.bySemanticsLabel('Marcar ponto')),
          matchesSemantics(
            label: 'Marcar ponto',
            isButton: true,
            hasEnabledState: true,
            isEnabled: false,
          ),
        );
      },
    );
  });
}

void _ignore(AppTab tab) {}
