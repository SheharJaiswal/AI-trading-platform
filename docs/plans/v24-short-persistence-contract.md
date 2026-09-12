# V24 — Durable paper short persistence contract

## Purpose
Define the minimum persistence contract needed to carry a paper short position from open through partial/full cover without inventing broker or live-trading behavior.

## Lifecycle

```text
SHORT_OPEN
   |
   +--> SHORT_PARTIALLY_COVERED
   |          |
   |          +--> SHORT_CLOSED
   |
   +--> SHORT_CLOSED
```

## Required persisted facts

A durable short position must retain:

- stable position identifier
- portfolio identifier
- symbol/instrument identity
- original open quantity
- remaining open quantity
- average entry price
- latest cover price when a cover is recorded
- cumulative realized P&L
- lifecycle state
- created/updated timestamps
- version/concurrency value where the persistence layer supports it

## Cover invariants

- entry and cover prices must be positive
- quantities must be positive for an operation
- cover quantity must not exceed remaining open quantity
- remaining quantity is `open - covered`
- realized P&L is `(entry - cover) * coveredQuantity`
- zero remaining quantity means `SHORT_CLOSED`
- positive remaining quantity after a valid cover means `SHORT_PARTIALLY_COVERED`
- realized P&L is persisted as a result of a paper fill, not predicted by AI

## Idempotency and reconciliation

A durable cover operation must have a caller-supplied idempotency key. Repeating the same key for the same cover operation must not apply the cover twice. A key associated with conflicting operation identity must fail closed. A persisted order/fill without a corresponding position update is a reconciliation condition rather than permission to retry blindly.

## Boundary and safety

This contract is paper-only. It does not authorize broker submission, live shorting, automatic liquidation, margin borrowing, overnight carry, or AI bypass of deterministic risk controls.

## Explicitly not decided here

This contract does not define starting capital, position sizing, margin requirements, fees/taxes, slippage, borrow availability, market-hours rules, automatic close policy, or live broker integration. Those remain product/business decisions and must not be inferred from this persistence contract.
