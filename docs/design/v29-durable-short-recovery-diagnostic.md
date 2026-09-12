# V29 Durable Short Recovery Diagnostic

## Goal

Expose the V27 durable paper-short reconciliation result through a read-only diagnostic endpoint backed by the persisted position and cover-operation ledger.

## Contract

`DurableShortRecoveryQueryService` loads one persisted short position and its cover ledger, then delegates validation to `DurableShortRecoveryReconciler`. The response includes the persisted position, ordered covers, and deterministic reconciliation result.

The endpoint is:

`GET /api/paper-shorts/{positionId}/recovery`

A missing position returns `404 SHORT_POSITION_NOT_FOUND`. When persistence is disabled, the endpoint returns `503 PERSISTENCE_DISABLED` because there is no durable ledger to inspect.

## Safety

This operation is strictly diagnostic. It performs no repair, retry, close, liquidation, broker call, or risk-control bypass. It does not mutate the position or cover ledger. It remains paper-only.
