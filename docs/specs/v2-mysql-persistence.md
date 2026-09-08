# V2 MySQL Persistence Specification

V2 replaces V1 in-memory trading state with durable MySQL state while preserving paper-only execution, buy-only risk policy, and provider-neutral domain contracts.

## Scope

- Durable orders, fills, portfolio, positions, alerts, and normalized market-data snapshots.
- Application-layer repository contracts; Infrastructure-layer EF Core/MySQL implementation.
- Explicit EF Core migrations; startup does not silently mutate production schema.
- Restart-safe restoration of portfolio, positions, alert deduplication, and market prices.
- Transactional paper execution and database-enforced alert idempotency.
- Optimistic concurrency protection for portfolio writes.

## Non-goals

- Real-money execution.
- Short selling or leverage.
- ML, news, fundamentals, autonomous trading, Redis, RabbitMQ, or event sourcing.

## Persistence invariants

- Approved BUY execution persists order, fill, cash change, and position change atomically.
- A retry cannot double-debit cash or create a second fill for the same order.
- Stop-loss alert identity remains `(position_id, rule, evaluation_bucket)` and is unique in the database.
- Unrealized P&L is derived from persisted position quantity, average entry price, and current market price.
- Broker credentials are never stored in MySQL.
- Angel One remains market-data-only in V2.

## Testing gate

CI must validate restore/build/test and migration model compilation. Integration tests must subsequently validate migrations, persistence, rollback, restart restoration, idempotency, and concurrency before V2 merge.

## Provider decision

Use `Pomelo.EntityFrameworkCore.MySql` 9.0.0 for the first V2 implementation. NuGet lists it as compatible with `net10.0`; its EF Core dependency is 9.x. This is intentional: target framework compatibility is separate from the EF Core provider version, and the CI build is the compatibility gate. citeturn0search0turn0search10
