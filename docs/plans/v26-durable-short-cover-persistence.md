# V26 — Durable short-cover persistence

## Scope
Persist the existing deterministic paper short lifecycle without changing unresolved trading business rules.

## Implemented

- durable short position state retains original quantity, remaining quantity, entry price, latest cover price, cumulative realized P&L, lifecycle state, timestamps, and version
- cover operations are persisted separately with a caller-supplied idempotency key
- a cover update and its operation record are committed atomically
- position reads used by cover are locked with `FOR UPDATE`
- optimistic version checking rejects stale callers
- replaying the same idempotency key with the same price/quantity returns the already-applied position without double-counting P&L
- reusing a key for different cover inputs fails closed
- partial and full covers preserve the deterministic V24 lifecycle

## Boundary

This is paper-only persistence. It does not add live broker submission, automatic liquidation, margin borrowing, overnight carry, or AI bypass of deterministic risk controls.

## Not changed

Starting capital, sizing, fees/taxes, slippage, borrow rules, market-hours rules, automatic close policy, and live execution remain unresolved product decisions.
