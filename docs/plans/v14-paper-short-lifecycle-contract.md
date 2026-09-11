# V14 Paper Short Lifecycle Contract

## Objective

Close the current autonomous-paper-cycle gap around bearish intraday candidates without enabling live brokerage execution.

## Current baseline

The autonomous paper cycle ranks configured symbols and currently sends only a BUY candidate through the existing paper execution and risk boundary. HOLD/NoDecision candidates do not create orders.

## Proposed safe lifecycle

A future implementation should support a complete paper-only lifecycle for both directions:

- LONG entry: BUY
- LONG exit: SELL
- SHORT entry: SELL/SHORT
- SHORT exit: BUY/COVER

The system must never infer a live broker action from these states.

## Required position state

Every simulated position should expose:

- symbol
- side (LONG or SHORT)
- quantity
- entry price
- current price
- stop-loss
- target
- unrealized P&L
- lifecycle status
- strategy/version identifier
- timestamps

## Risk gate

No entry or exit should bypass the existing deterministic risk authority. A missing or invalid stop-loss/target configuration must result in `NO_TRADE` rather than an invented level.

For a paper short, the risk engine must treat adverse upward movement as loss and downward movement as favorable movement. Stop-loss and target evaluation must use causal market data only; future candles must never influence a decision.

## Exit semantics

The future implementation should distinguish:

1. `SIGNAL_SHORT` — bearish setup identified, not yet executed.
2. `SHORT_OPEN` — paper short accepted by the deterministic execution/risk boundary.
3. `SHORT_STOPPED` — stop-loss condition observed.
4. `SHORT_TARGET_HIT` — target condition observed.
5. `SHORT_CLOSED` — explicit paper close.
6. `NO_TRADE` — setup rejected or insufficient evidence.

## Safety constraints

- Paper-only.
- No live broker adapter.
- No automatic liquidation policy is introduced by this contract.
- No new market-hours policy.
- No invented risk thresholds.
- AI remains advisory metadata only.
- Deterministic risk remains authoritative.
- Provider failures must not create an order.
- Duplicate cycle execution must remain idempotent.

## Acceptance criteria for implementation

- Existing long paper workflows remain backward compatible.
- A bearish candidate can be represented without immediately creating an order.
- A validated short can be opened only through the existing deterministic paper execution boundary.
- Stop-loss and target are persisted with the paper position.
- Short P&L and exit semantics are tested.
- Duplicate cycle calls do not double-enter the same signal.
- Provider failure produces `NO_TRADE`/failure state and never an order.
- .NET and Angular/Docker CI pass on the exact implementation head before merge.
