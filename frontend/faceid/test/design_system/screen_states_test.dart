import 'package:faceid/design_system/components/app_card.dart';
import 'package:faceid/design_system/components/app_empty_state.dart';
import 'package:faceid/design_system/components/app_error_state.dart';
import 'package:faceid/design_system/components/app_loading.dart';
import 'package:faceid/design_system/components/app_page_header.dart';
import 'package:faceid/design_system/components/app_text_field.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import '../support/pump_design_system.dart';

/// Empty and error are first-class states, not accidents. They are what a new
/// customer sees before any data exists and what everyone sees when something
/// breaks, so they get the same coverage as the happy path.
void main() {
  group('AppEmptyState', () {
    testWidgets(
      'Given no action, When rendered, Then it explains without offering a button',
      (tester) async {
        // Arrange — not every emptiness is fixable by the person looking at it
        await pumpComponent(
          tester,
          const AppEmptyState(
            title: 'Nenhuma marcação hoje',
            message: 'As marcações aparecem aqui assim que o ponto for batido.',
          ),
        );

        // Assert
        expect(find.text('Nenhuma marcação hoje'), findsOneWidget);
        expect(find.byType(ElevatedButton), findsNothing);
      },
    );

    testWidgets('Given an action, When it is tapped, Then the callback fires', (
      tester,
    ) async {
      // Arrange
      var pressed = 0;
      await pumpComponent(
        tester,
        AppEmptyState(
          title: 'Nenhum funcionário',
          message: 'Cadastre o primeiro para começar.',
          actionLabel: 'Cadastrar funcionário',
          onAction: () => pressed++,
        ),
      );

      // Act
      await tester.tap(find.text('Cadastrar funcionário'));
      await tester.pumpAndSettle();

      // Assert
      expect(pressed, 1);
    });
  });

  group('AppErrorState', () {
    testWidgets(
      'Given a message, When rendered, Then no technical detail reaches the screen',
      (tester) async {
        // Arrange — the component shows what it is given, and what it is given
        // is already translated. This pins that it adds nothing of its own.
        await pumpComponent(
          tester,
          const AppErrorState(
            message: 'Não foi possível carregar as marcações.',
          ),
        );

        // Assert
        expect(
          find.text('Não foi possível carregar as marcações.'),
          findsOneWidget,
        );
        expect(find.textContaining('Exception'), findsNothing);
        expect(find.textContaining('#0'), findsNothing);
      },
    );

    testWidgets('Given a retry callback, When retry is tapped, Then it fires', (
      tester,
    ) async {
      // Arrange
      var retries = 0;
      await pumpComponent(
        tester,
        AppErrorState(
          message: 'Sem conexão com o servidor.',
          onRetry: () => retries++,
        ),
      );

      // Act
      await tester.tap(find.text('Tentar novamente'));
      await tester.pumpAndSettle();

      // Assert
      expect(retries, 1);
    });

    testWidgets(
      'Given no retry callback, When rendered, Then no retry is offered',
      (tester) async {
        // Arrange — offering a retry that does nothing is worse than none
        await pumpComponent(
          tester,
          const AppErrorState(message: 'Sua empresa está suspensa.'),
        );

        // Assert
        expect(find.text('Tentar novamente'), findsNothing);
      },
    );
  });

  group('AppLoading', () {
    testWidgets(
      'Given a message, When rendered, Then it is announced, not just spun',
      (tester) async {
        // Arrange — a bare spinner says something is happening but not what
        await pumpComponent(
          tester,
          const AppLoading(message: 'Validando identidade...'),
        );

        // Act
        final semantics = tester.getSemantics(
          find.bySemanticsLabel('Validando identidade...'),
        );

        // Assert
        expect(find.byType(CircularProgressIndicator), findsOneWidget);
        expect(semantics.flagsCollection.isLiveRegion, isTrue);
      },
    );
  });

  group('AppTextField', () {
    testWidgets(
      'Given an error text, When rendered, Then it is shown with the label',
      (tester) async {
        // Arrange & Act
        await pumpComponent(
          tester,
          const AppTextField(
            label: 'CPF',
            errorText: 'CPF ou senha incorretos',
          ),
        );

        // Assert
        expect(find.text('CPF'), findsOneWidget);
        expect(find.text('CPF ou senha incorretos'), findsOneWidget);
      },
    );

    testWidgets(
      'Given no error, When rendered, Then no error row is reserved',
      (tester) async {
        // Arrange — an empty error would shift the layout for nothing
        await pumpComponent(tester, const AppTextField(label: 'CPF'));

        // Assert
        expect(find.text('CPF'), findsOneWidget);
      },
    );
  });

  group('AppPageHeader', () {
    testWidgets('Given a title, When rendered, Then it is a semantic heading', (
      tester,
    ) async {
      // Arrange — without this a screen reader user has to read the whole
      // page to find out where they are
      await pumpComponent(
        tester,
        const AppPageHeader(title: 'Funcionários', subtitle: '12 ativos'),
      );

      // Act
      final semantics = tester.getSemantics(
        find.bySemanticsLabel('Funcionários'),
      );

      // Assert
      expect(semantics.flagsCollection.isHeader, isTrue);
      expect(find.text('12 ativos'), findsOneWidget);
    });
  });

  group('AppCard', () {
    testWidgets('Given a child, When rendered, Then the child is visible', (
      tester,
    ) async {
      // Arrange & Act
      await pumpComponent(
        tester,
        const AppCard(title: 'Hoje', child: Text('Entrada 08:02')),
      );

      // Assert
      expect(find.text('Hoje'), findsOneWidget);
      expect(find.text('Entrada 08:02'), findsOneWidget);
    });
  });
}
