# Persistence Implementation Plan

## Phase 1 — Database foundation

- Add EF Core/Npgsql packages to Infrastructure.
- Introduce `TradingDbContext` and persistence entities.
- Add PostgreSQL configuration without committing secrets.
- Add initial migration.
- Add local Docker Compose PostgreSQL configuration if the repository does not already provide one.

## Phase 2 — Repositories

Implement application-facing repository interfaces and Infrastructure adapters for:

- portfolio
- position
- paper order
- fill
- alerts
- market snapshots
- recommendation history

Keep domain models independent of EF Core entities.

## Phase 3 — Atomic paper execution

Replace `PaperPortfolio` persistence with database-backed state.

Execution flow:

`Recommendation → RiskEngine → transaction → order → fill → cash/position update → commit`

Add concurrency protection and transaction tests.

## Phase 4 — Durable monitoring

Replace the in-memory alert store with PostgreSQL.

Risk monitoring must remain safe to run repeatedly. The database unique constraint on `AlertKey` is the final idempotency boundary.

## Phase 5 — Market/recommendation history

Persist normalized market snapshots and recommendation records. Preserve strategy version and analysis inputs needed for later evaluation.

## Phase 6 — Integration validation

Run PostgreSQL integration tests in CI using an isolated service/container. Validate migrations, transaction rollback, concurrent execution, alert idempotency, and restart/reload behavior.

## Phase 7 — API compatibility

Keep existing V1 endpoints stable while switching their backing store from memory to PostgreSQL.

## Delivery rule

Implement one phase at a time on a feature branch, keep commits focused, and open a PR against `main`. Do not merge until the persistence specification is reviewed, CI is green, findings are resolved, and merge approval is explicit.
