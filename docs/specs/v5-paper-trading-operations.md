# V5 Paper Trading Operations Specification

## Goal

Turn the V4 research/backtesting baseline into an operational paper-trading workflow that can safely move from historical simulation to repeatable, observable paper execution without enabling live brokerage orders.

## Scope

- Paper-trading session lifecycle: create, start, pause, resume and stop.
- Explicit strategy/risk configuration and immutable session assumptions.
- Deterministic signal-to-paper-order workflow using the existing recommendation and risk engines.
- Idempotent paper-order handling and duplicate-event protection.
- Persistent paper-order, fill and decision audit records linked to a session.
- Portfolio/equity snapshots suitable for operational monitoring.
- Clear separation between backtest, paper execution and any future live broker integration.
- Angular paper-trading operations workspace with session state, orders, fills, risk decisions and portfolio/equity views.
- Operational API validation, failure states and audit retrieval.

## Non-goals

- Live brokerage execution.
- Autonomous real-money trading.
- AI authorization of orders.
- ML training or strategy optimization.
- Leverage, derivatives or short selling.
- Microservice decomposition or new messaging infrastructure.

## Safety invariants

1. V5 remains paper-only; no live broker order endpoint may be called.
2. Deterministic risk checks remain authoritative.
3. AI output is advisory metadata only and cannot create, approve or modify an order.
4. Every paper order has an idempotency identity and auditable decision trail.
5. Session configuration is immutable after start.
6. Duplicate market events cannot create duplicate paper orders.
7. Portfolio mutation occurs only through the existing paper-execution boundary.
8. Backtest state and paper-session state remain isolated.
9. Operational failures are explicit; no silent order acceptance.
10. The UI must visibly distinguish paper execution from historical simulation and live trading.

## User workflow

1. Configure a paper-trading session with symbol universe, interval and strategy version.
2. Review deterministic risk and execution assumptions.
3. Start the session.
4. Observe recommendations, risk decisions, paper orders, fills and portfolio/equity state.
5. Pause/resume or stop the session explicitly.
6. Inspect immutable audit history after the session.

## Acceptance criteria

- A session cannot transition through invalid lifecycle states.
- Repeated processing of the same market event is idempotent.
- Risk-blocked decisions never create paper orders.
- Accepted paper orders create auditable order/fill records.
- Portfolio accounting remains consistent with paper fills.
- Session configuration and strategy version are persisted.
- Backtest execution remains unaffected by V5 session operations.
- AI cannot bypass deterministic risk authorization.
- Angular tests cover lifecycle, errors, audit and paper-only labeling.
- Combined .NET + Angular CI is green before merge.

## Milestones

### V5.1 — Session lifecycle and configuration

Define session contracts, lifecycle state machine, persistence and API validation.

### V5.2 — Operational paper execution

Integrate the existing deterministic recommendation/risk path with idempotent paper execution and audit events.

### V5.3 — Monitoring and audit

Add session dashboard, orders/fills/decisions and portfolio/equity snapshots.

### V5.4 — Validation and acceptance

Add integration/UI safety tests, run CI, and complete BA, Senior Engineer and Trader/Safety acceptance.
