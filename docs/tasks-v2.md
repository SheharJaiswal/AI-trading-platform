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

- [ ] Persist alerts.
- [ ] Enforce database stop-loss uniqueness.
- [ ] Restore alert state after restart.
- [ ] Persist latest market price.

## Phase 5 — market-data audit

- [ ] Persist normalized quote snapshots.
- [ ] Preserve provider/exchange timestamps.

## Final gate

- [ ] MySQL integration tests.
- [ ] CI green on final implementation commit.
- [ ] BA/Senior Engineer/Trader acceptance review.
- [ ] Explicit merge approval.
