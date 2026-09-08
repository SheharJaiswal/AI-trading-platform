# V2 MySQL Persistence Specification

## Objective

Replace V1 in-memory portfolio, order/fill audit, and alert state with durable MySQL persistence while preserving V1 paper-trading safety and provider boundaries.

## Scope

1. Persist paper orders and fills.
2. Persist portfolio cash, positions, realized P&L, and mark-to-market state.
3. Persist monitoring alerts and their idempotency identity.
4. Persist normalized market-data snapshots needed for audit/restart continuity.
5. Introduce repository interfaces in the Application layer and MySQL implementations in Infrastructure.
6. Add EF Core migrations and deterministic schema creation through migrations only.
7. Restore portfolio and alert state on API/Worker restart.
8. Preserve V1 buy-only, paper-only execution and risk gates.
9. Add transaction boundaries for financial state changes and alert creation.
10. Add integration tests against MySQL-compatible infrastructure without requiring live broker credentials.

## Non-goals

- Real-money order execution.
- Short selling or leverage.
- ML/prediction persistence.
- News/fundamental persistence.
- Redis/RabbitMQ.
- Event sourcing.
- Replacing domain rules with database logic.

## Architecture

- Domain remains persistence-ignorant.
- Application owns repository contracts and orchestration.
- Infrastructure owns EF Core, DbContext, entity mappings, migrations, and MySQL implementation.
- API and Worker consume Application services; neither accesses DbContext directly.
- MySQL is the V2 system of record for paper-trading state.

## Persistence model

### orders

- id (UUID primary key)
- symbol
- instrument_token nullable
- side
- quantity
- limit_price
- strategy_version
- created_at
- execution_mode (`paper`)
- status

### fills

- id (UUID primary key)
- order_id foreign key
- symbol
- side
- quantity
- fill_price
- filled_at
- execution_provider (`paper`)
- unique order/fill identity as appropriate for retry safety

### positions

- id (UUID primary key)
- symbol
- instrument_token nullable
- quantity
- average_entry_price
- current_market_price
- opened_at
- updated_at

V2 remains buy-only; a position is removed when its quantity reaches zero.

### portfolio

A single paper portfolio is sufficient for V2.

- id (UUID primary key)
- cash
- realized_pnl
- updated_at
- concurrency/version field

Unrealized P&L remains derived from persisted position quantity, average entry price, and current market price rather than stored as an independently mutable total.

### alerts

- id (UUID primary key)
- position_id foreign key nullable
- rule
- severity
- message
- evaluation_bucket
- created_at
- unique identity on `(position_id, rule, evaluation_bucket)` for stop-loss alerts
- status/delivery state reserved for future notification channels

### market_data_snapshots

- id (UUID primary key)
- symbol
- instrument_token nullable
- provider
- exchange
- provider_timestamp
- received_at
- open/high/low/close/last_traded_price
- volume

Provider timestamps must remain distinct from application receipt timestamps.

## Transaction and consistency rules

- Approved paper BUY execution must atomically persist the order, fill, cash change, and position change.
- A failed transaction must not leave a partially applied portfolio state.
- Retry of an already-completed paper execution must not create a second fill or double-debit cash.
- Stop-loss alert creation must be idempotent using the defined unique identity.
- Portfolio writes must use optimistic concurrency or equivalent protection against lost updates.
- Reads used for execution/risk decisions must observe a consistent portfolio state.

## Restart behavior

After restart:

- portfolio cash and realized P&L are restored from MySQL;
- open positions and their latest market prices are restored;
- alert history and stop-loss deduplication state are restored;
- no paper order is replayed merely because the Worker restarted;
- monitoring may safely reevaluate open positions.

## Configuration

Required configuration:

- `ConnectionStrings:MySql`
- database provider selection must be explicit for production-like environments.

No credentials are committed. Local development may use environment variables or user secrets.

## Migration policy

- EF Core migrations are committed to source control.
- Application startup must not silently mutate production schema.
- CI validates that migrations build successfully.
- Schema changes require a new migration.

## Testing acceptance criteria

- Domain tests remain green.
- Application tests verify repository orchestration and transactional behavior through fakes where appropriate.
- Infrastructure integration tests verify MySQL mappings, migrations, unique constraints, persistence, restart restoration, and concurrency behavior.
- Tests must prove duplicate execution cannot double-debit cash or create duplicate fills.
- Tests must prove duplicate stop-loss evaluations create one alert per identity bucket.
- CI must pass without live Angel One credentials.

## Security and safety acceptance criteria

- No live broker order endpoint is introduced.
- No broker credentials are persisted in MySQL.
- No API endpoint can bypass the risk gate to create a live order.
- Paper execution remains the only execution provider in V2.

## Definition of Done

V2 is complete only when the MySQL schema and migrations are committed, repository abstractions are implemented, API and Worker use durable state, restart behavior is tested, idempotency/concurrency tests pass, CI is green, and BA/Senior Engineer/Trader acceptance review finds no unresolved V2 requirement gaps.

## Implementation decision

Use EF Core with the Pomelo MySQL provider in Infrastructure. Provider compatibility must be verified by CI before merge.
