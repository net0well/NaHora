import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:nahora/core/errors/app_exception.dart';
import 'package:nahora/features/adjustments/domain/adjustment_kind.dart';
import 'package:nahora/features/adjustments/presentation/adjustments_providers.dart';
import 'package:nahora/features/adjustments/presentation/manager_queue.dart';

import '../../support/fake_adjustments_repository.dart';
import '../../support/pump_design_system.dart';

/// The manager's queue.
///
/// A decision here is what makes a corrected time count, so the interesting
/// cases are the ones where a tap must not become a decision: a stale request,
/// a second tap, a failure the manager cannot see.
void main() {
  group('ManagerQueue', () {
    late FakeAdjustmentsRepository repository;

    setUp(() {
      repository = FakeAdjustmentsRepository();
    });

    Future<void> pump(WidgetTester tester) => pumpScreen(
      tester,
      const Scaffold(body: SingleChildScrollView(child: ManagerQueue())),
      size: const Size(390, 1400),
      overrides: [adjustmentsRepositoryProvider.overrideWithValue(repository)],
    );

    testWidgets('Given nothing pending, When shown, '
        'Then it says so instead of rendering an empty list', (tester) async {
      // Arrange & Act
      await pump(tester);

      // Assert
      expect(find.byKey(const Key('manager_queue_empty')), findsOneWidget);
    });

    testWidgets(
      'Given a request, When shown, Then the change reads in company time',
      (tester) async {
        // Arrange — the server sends UTC and São Paulo is three hours behind,
        // so 11:32Z becoming 11:05Z has to read as 08:32 becoming 08:05
        repository.requests = [FakeAdjustmentsRepository.pending()];

        // Act
        await pump(tester);

        // Assert
        expect(
          find.byKey(const Key('adjustment_card_adjustment-1')),
          findsOneWidget,
        );
        expect(find.text('Joana Funcionária'), findsOneWidget);
        expect(find.text('30/08/2026 · De 08:32 para 08:05'), findsOneWidget);
        expect(find.text('Esquecimento de marcação'), findsOneWidget);
      },
    );

    testWidgets('Given an inclusion, When shown, '
        'Then no original is invented for it', (tester) async {
      // Arrange — nothing was recorded, so there is nothing to show beside the
      // new time; a dash there reads as a value somebody could try to fix
      repository.requests = [
        FakeAdjustmentsRepository.pending(kind: AdjustmentKind.inclusion),
      ];

      // Act
      await pump(tester);

      // Assert
      expect(find.text('30/08/2026 · Incluir às 08:05'), findsOneWidget);
      expect(find.textContaining('para 08:05'), findsNothing);
    });

    testWidgets(
      'Given Aprovar is tapped, When it succeeds, Then the request is approved',
      (tester) async {
        // Arrange
        repository.requests = [FakeAdjustmentsRepository.pending()];
        await pump(tester);

        // Act
        await tester.tap(
          find.byKey(const Key('adjustment_approve_adjustment-1')),
        );
        await tester.pumpAndSettle();

        // Assert
        expect(repository.decisions, [
          (adjustmentId: 'adjustment-1', approve: true),
        ]);
        expect(find.byKey(const Key('manager_queue_empty')), findsOneWidget);
      },
    );

    testWidgets('Given Recusar is tapped, When it succeeds, '
        'Then the refusal is sent as a refusal', (tester) async {
      // Arrange — the two buttons sit side by side, and sending the wrong one
      // silently alters somebody's payroll
      repository.requests = [FakeAdjustmentsRepository.pending()];
      await pump(tester);

      // Act
      await tester.tap(find.byKey(const Key('adjustment_reject_adjustment-1')));
      await tester.pumpAndSettle();

      // Assert
      expect(repository.decisions, [
        (adjustmentId: 'adjustment-1', approve: false),
      ]);
    });

    testWidgets('Given the request was already decided, When Aprovar is '
        'tapped, Then the manager is told why nothing happened', (
      tester,
    ) async {
      // Arrange — two managers open the same queue; the second one must not be
      // left looking at a button that did nothing
      repository.requests = [FakeAdjustmentsRepository.pending()];
      repository.decisionFailure = const BusinessException(
        'ADJUSTMENT_ALREADY_DECIDED',
      );
      await pump(tester);

      // Act
      await tester.tap(
        find.byKey(const Key('adjustment_approve_adjustment-1')),
      );
      await tester.pumpAndSettle();

      // Assert
      expect(find.byKey(const Key('manager_queue_error')), findsOneWidget);
      expect(find.text('Esta solicitação já foi decidida.'), findsOneWidget);
    });

    testWidgets('Given a decision in flight, When another is tapped, '
        'Then only the first one is sent', (tester) async {
      // Arrange — a list of buttons is a list of ways to double-submit
      final gate = Completer<void>();
      repository
        ..requests = [
          FakeAdjustmentsRepository.pending(),
          FakeAdjustmentsRepository.pending(
            id: 'adjustment-2',
            employeeName: 'Pedro Funcionário',
          ),
        ]
        ..decisionGate = gate.future;
      await pump(tester);

      // Act
      await tester.tap(
        find.byKey(const Key('adjustment_approve_adjustment-1')),
      );
      await tester.pump();
      await tester.tap(
        find.byKey(const Key('adjustment_approve_adjustment-2')),
        warnIfMissed: false,
      );
      await tester.pump();

      // Assert
      expect(repository.decisions, [
        (adjustmentId: 'adjustment-1', approve: true),
      ]);

      gate.complete();
      await tester.pumpAndSettle();
    });

    testWidgets(
      'Given the queue cannot be read, When shown, Then it offers a retry',
      (tester) async {
        // Arrange
        repository.failure = const NetworkException();

        // Act
        await pump(tester);

        // Assert
        expect(
          find.text('Não foi possível carregar as solicitações.'),
          findsOneWidget,
        );
      },
    );

    testWidgets('Given an attachment, When shown, '
        'Then its existence is stated without a link to it', (tester) async {
      // Arrange — a queue that linked straight to files would be a way to page
      // through somebody's medical certificates
      repository.requests = [
        FakeAdjustmentsRepository.pending(hasAttachment: true),
      ];

      // Act
      await pump(tester);

      // Assert
      expect(find.text('Comprovante anexado'), findsOneWidget);
    });
  });
}
