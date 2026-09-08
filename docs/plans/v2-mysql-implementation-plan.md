# V2 MySQL Implementation Plan

## Phase 1 — persistence foundation

1. Add EF Core/Pomelo dependencies to Infrastructure.
2. Add `TradingDbContext` and explicit entity mappings.
3. Add MySQL configuration and connection-string validation.
4. Add initial migration.
5. Keep domain entities independent from EF Core types.

## Phase 2 — repository contracts

1. Define portfolio repository contract.
2. Define order/fill repository contract.
3. Define alert repository contract.
4. Define market-data snapshot repository contract.
5. Define transaction/unit-of-work boundary where needed.

## Phase 3 — durable paper trading

1. Replace in-memory portfolio persistence in API with MySQL-backed implementation.
2. Persist approved paper orders and fills atomically with portfolio changes.
3. Restore open positions and cash on startup.
4. Preserve current risk gate and paper-only execution provider.
5. Add idempotency protection for repeated execution attempts.

## Phase 4 — durable monitoring

1. Persist alerts.
2. Enforce stop-loss uniqueness at database level.
3. Restore alert state after Worker restart.
4. Persist latest market price required for unrealized P&L.
5. Keep monitoring cancellation-safe and retry-safe.

## Phase 5 — market-data audit

1. Persist normalized quote snapshots.
2. Preserve provider and exchange timestamps.
3. Keep receipt time separate from provider event time.
4. Avoid making historical snapshots an execution dependency unless explicitly required by a later spec.

## Phase 6 — integration and acceptance

1. Add MySQL integration test infrastructure.
2. Test migrations on a clean database.
3. Test restart restoration.
4. Test transaction rollback.
5. Test duplicate execution/idempotency.
6. Test stop-loss alert uniqueness.
7. Run CI.
8. Perform BA + Senior Engineer + Trader review.
9. Request explicit merge approval.

## Delivery rule

Do not add dashboard, ML, news, autonomous trading, or Redis/RabbitMQ work to this slice. V2 is specifically the persistence boundary required to make the V1 paper-trading workflow durable and restart-safe.
