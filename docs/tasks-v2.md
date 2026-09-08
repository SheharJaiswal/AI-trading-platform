# V2 MySQL Persistence Delivery Checklist

## Spec gate

- [x] V2 MySQL persistence specification.
- [x] V2 implementation plan.
- [x] Dedicated implementation branch.

## Phase 1 — persistence foundation

- [x] Add Pomelo MySQL EF Core provider.
- [x] Add `TradingDbContext`.
- [x] Add explicit entity mappings and constraints.
- [x] Add design-time DbContext factory.
- [x] Add initial migration.
- [x] CI restore/build/test against V2 branch.
- [x] Review migration/model against specification.

## Phase 2 — repository contracts

- [x] Portfolio repository.
- [x] Order/fill repository.
- [x] Alert repository.
- [x] Market-data snapshot repository.
- [x] Transaction boundary.

## Phase 3 — durable paper trading

- [x] Persist approved paper execution atomically.
- [x] Persist/restore portfolio and positions.
- [x] Prevent duplicate execution/double debit.
- [x] Preserve V1 risk gate.
- [x] Replace API paper-trade execution path with durable service.
- [x] Require API `Idempotency-Key`.
- [x] Add API contract tests for idempotency.

## Phase 4 — durable monitoring

- [x] Persist alerts.
- [x] Enforce database stop-loss uniqueness.
- [x] Restore alert state after restart.
- [x] Persist latest market price.
- [x] Wire durable monitoring into the background worker when MySQL persistence is enabled.
- [x] Add MySQL integration coverage for stop-loss alert uniqueness and durable market-price updates.

## Phase 5 — market-data audit

- [x] Persist normalized quote snapshots from durable monitoring.
- [x] Preserve provider/exchange timestamps in persisted snapshots.
- [x] Verify normalized snapshot values through MySQL integration tests.

## Final gate

- [x] MySQL migration/schema integration tests.
- [ ] CI green on final implementation commit.
- [ ] BA/Senior Engineer/Trader acceptance review.
- [ ] Explicit merge approval.
