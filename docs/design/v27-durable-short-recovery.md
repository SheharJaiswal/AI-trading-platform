# V27 Durable Short Recovery Reconciliation

## Goal

Provide a deterministic restart/recovery consistency check for durable paper-short positions and their persisted cover-operation ledger.

## Contract

`DurableShortRecoveryReconciler` is a pure validation component. It compares persisted position state with persisted cover operations and returns either `CONSISTENT` or a deterministic mismatch reason.

The check validates:

- position quantity bounds and positive entry price
- cover ownership and positive cover values
- unique idempotency keys
- no over-cover
- remaining quantity
- accumulated realized P&L
- position version
- sequential cover versions
- lifecycle state
- last cover price

For an opened position with no cover operations, the expected lifecycle state is `SHORT_OPEN` and the expected version is `0`. Each persisted cover must then advance the position version exactly once in chronological ledger order.

## Recovery behavior

A mismatch is reported; this component never mutates state, retries a cover, closes a position, liquidates anything, or calls a broker. A future orchestration layer may decide how to surface or quarantine an inconsistency, but no repair policy is invented here.

## Safety

V27 remains paper-only. It introduces no live broker execution, margin borrowing, automatic liquidation, overnight carry, or AI bypass of deterministic controls.
