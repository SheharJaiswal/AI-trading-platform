# V7 Live Trading Specification

## Goal

Define the safety, execution, reconciliation, and operational contracts required before the platform can route an explicitly authorized order to a real broker.

V7 is a specification and architecture phase. It does not enable live trading by itself.

## Scope

- Introduce an explicit live execution mode separate from paper execution.
- Define a provider-neutral broker execution boundary.
- Define order submission, acknowledgement, rejection, cancellation, partial-fill, and fill-state contracts.
- Define idempotency and duplicate-order prevention at the live execution boundary.
- Define broker-to-platform position and order reconciliation.
- Define credential and secret handling without persisting broker secrets in application data.
- Define explicit operator controls for enabling/disabling live execution.
- Preserve deterministic risk as authoritative before live submission.
- Preserve paper-only and backtest isolation until live implementation is explicitly enabled.

## Non-goals

- No broker adapter implementation in this specification change.
- No automatic liquidation.
- No AI authorization of live orders.
- No bypass of deterministic risk controls.
- No always-on autonomous live trading.
- No assumption of a specific broker, exchange, asset class, or authentication mechanism.

## Execution boundary

Strategy/Recommendation -> Deterministic Risk -> Explicit Execution Command -> Execution Provider

Providers remain replaceable:

- PaperExecutionProvider
- Future LiveBrokerExecutionProvider

Backtesting must not depend on either provider.

## Live order safety contract

A live order may only be submitted when all required conditions are satisfied:

1. Live execution mode is explicitly enabled.
2. The request has an explicit idempotency key.
3. Symbol/instrument identity is validated.
4. Quantity and price constraints are valid.
5. Deterministic risk approval is current.
6. Account/execution context is explicit.
7. The application is not operator-disabled or fail-closed.
8. The provider is healthy enough to accept the request.

Any failed prerequisite produces a no-submission outcome.

## Idempotency

The live boundary must prevent duplicate broker submissions caused by retries, restart, network timeout after broker acceptance, worker retry, or concurrent requests.

Durable execution identity must distinguish:

- confirmed broker submission;
- confirmed broker rejection;
- unknown submission state requiring reconciliation.

An unknown state must not be blindly resubmitted.

## Reconciliation

The platform must reconcile durable local state with broker state for orders, fills, open positions, and cancellations/rejections.

Uncorrelated or divergent state must be observable and fail closed where safe correlation is impossible.

## Failure handling

The design must explicitly cover broker unavailability, authentication failure, timeout after submission, duplicate/unknown broker response, partial fill, rejection, cancellation, stale market data, stale risk approval, local persistence failure, and broker/local divergence.

No failure path may silently create a second live order.

## Operator controls

Live execution requires explicit controls for enable/disable, account/environment selection, emergency stop/fail-closed state, reconciliation status, and unresolved execution state.

These controls are independent of AI output.

## AI boundary

AI remains advisory metadata only. It cannot authorize live execution, bypass deterministic risk, change risk limits, enable live trading, or resolve broker/local reconciliation conflicts.

## Paper/backtest isolation

- Backtests cannot submit broker orders.
- Paper execution cannot implicitly route to a live provider.
- Live execution cannot be reached implicitly from backtest or research requests.
- AI research cannot directly invoke execution.
- Live execution requires an explicit execution-mode boundary.

## Implementation sequence

1. Add execution-mode separation contracts and tests.
2. Define provider-neutral live execution contracts.
3. Define durable live order state and idempotency.
4. Define reconciliation contracts and state machine.
5. Implement operator controls and fail-closed behavior.
6. Implement a broker adapter behind the provider boundary.
7. Add broker sandbox integration tests.
8. Keep production live routing disabled until all safety acceptance criteria pass.

## Acceptance criteria

- Paper and backtest behavior remains unchanged.
- No live provider is reachable without explicit live mode.
- Duplicate submissions are prevented across retry/restart/timeout scenarios.
- Unknown broker submission state requires reconciliation rather than retry.
- Deterministic risk remains authoritative.
- Broker/local order and position divergence is detectable and observable.
- AI cannot authorize or bypass execution controls.
- Credentials are not persisted as ordinary trading-domain state.
- Live execution can be disabled fail-closed.
- Broker adapters are replaceable without changing strategy, risk, or backtest logic.
