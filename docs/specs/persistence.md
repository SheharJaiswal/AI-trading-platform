# V1 Persistence Specification

**Status:** CONFIRMED FOR IMPLEMENTATION  
**Scope:** V1 paper-trading platform persistence

## 1. Goal

Replace V1 in-memory portfolio, position, paper-order, fill, and alert state with durable PostgreSQL persistence while preserving the existing domain and provider boundaries.

A process restart must not lose paper-trading state, realized/unrealized P&L history, or risk alerts.

## 2. Technology

- PostgreSQL is the system of record.
- TimescaleDB is optional and should be introduced only for high-volume market/time-series workloads.
- Entity Framework Core is the default .NET persistence technology unless a later specification identifies a concrete need for another approach.
- Database access belongs in `AiTrading.Infrastructure`.
- Domain and Application projects must not depend on PostgreSQL-specific types.
- Connection strings and credentials must come from configuration/secrets, never source control.

## 3. Persistence boundaries

Persist at minimum:

1. Paper portfolios
2. Positions
3. Paper orders
4. Fills
5. Risk alerts
6. Market-data snapshots required for reproducibility of recommendations and paper execution
7. Strategy/recommendation records needed to explain why a paper trade occurred

The application layer remains responsible for business rules. Persistence must not silently implement trading or risk decisions.

## 4. Required entities

### Portfolio

- `Id` UUID primary key
- `Name`
- `InitialCash`
- `CashBalance`
- `CreatedAt`
- `UpdatedAt`

### Position

- `Id` UUID primary key
- `PortfolioId` foreign key
- `Symbol`
- `Exchange`
- `InstrumentToken`
- `Quantity`
- `AverageEntryPrice`
- `StopLossPrice` nullable
- `OpenedAt`
- `UpdatedAt`
- optional closed timestamp/status as required by implementation

### PaperOrder

- `Id` UUID primary key
- `PortfolioId` foreign key
- `PositionId` nullable foreign key
- `Symbol`
- `Side`
- `Quantity`
- `RequestedPrice`
- `Status`
- `CreatedAt`
- `ExecutedAt` nullable
- `StrategyVersion`
- risk decision/reason

### Fill

- `Id` UUID primary key
- `OrderId` foreign key
- `Quantity`
- `Price`
- `ExecutedAt`
- `Fees` default zero for the initial implementation

### RiskAlert

- `Id` UUID primary key
- `AlertKey` unique
- `PortfolioId`
- `PositionId` nullable
- `Symbol`
- `Severity`
- `Type`
- `Message`
- `CreatedAt`
- `AcknowledgedAt` nullable

The unique `AlertKey` preserves idempotency for repeated monitoring cycles.

### MarketSnapshot

- `Id` UUID primary key
- `Symbol`
- `Exchange`
- `InstrumentToken`
- `Timestamp`
- OHLC
- `LastTradedPrice`
- `Volume`
- `Source`

A unique/index strategy must support symbol + timestamp queries efficiently.

### RecommendationRecord

- `Id` UUID primary key
- `Symbol`
- `Timestamp`
- `Action`
- `ReferencePrice`
- `ExpectedReturn` nullable
- `Confidence`
- `HorizonTradingDays`
- `StrategyVersion`
- serialized supporting signals/risk factors in a structured JSON column where appropriate

Recommendations are historical records, not mutable current state.

## 5. Accounting invariants

The persistence implementation must preserve these invariants:

- Cash cannot become negative through an approved paper BUY.
- Position quantity cannot become negative.
- A SELL cannot exceed available position quantity.
- Every executed order has exactly one or more corresponding fills; V1 uses one fill per order.
- Portfolio cash and positions are updated atomically with an executed fill.
- Realized P&L is derived from persisted execution history and/or persisted accounting fields with a clearly defined source of truth.
- Replaying the same monitoring event must not create duplicate risk alerts.

## 6. Transaction boundaries

A paper execution transaction must atomically persist:

`Order execution → Fill → Portfolio cash update → Position update`

If any operation fails, none of these state changes may be committed.

Risk evaluation occurs before the transaction and remains deterministic.

## 7. Concurrency

The implementation must protect portfolio cash and position quantities from concurrent paper-trade updates.

At minimum:
- use database transactions for execution;
- use optimistic concurrency/versioning or an equivalent safe mechanism for mutable portfolio/position state;
- never rely only on process-local locks.

## 8. Market data retention

V1 should persist the normalized market snapshots consumed by recommendation and risk-monitoring workflows.

Historical candle storage should use a schema that can later be migrated to TimescaleDB without changing application/domain contracts.

Retention duration is an open operational parameter and must be configuration-driven rather than hardcoded into domain logic.

## 9. Migrations

- Database schema changes must be represented as EF Core migrations.
- Migrations are source-controlled.
- CI must validate that the solution builds and tests without requiring a developer's local database.
- Production migration execution must be an explicit deployment step, not an application startup side effect unless a later operational specification approves it.

## 10. Repository interfaces

Application-facing interfaces should express business intent, for example:

- `IPortfolioRepository`
- `IPositionRepository`
- `IPaperOrderRepository`
- `IFillRepository`
- `IRiskAlertRepository`
- `IMarketSnapshotRepository`
- `IRecommendationRepository`

Do not expose `DbContext`, EF entities, `NpgsqlConnection`, or provider-specific query objects outside Infrastructure.

## 11. API impact

Existing V1 API behavior should remain compatible where practical:

- `GET /api/portfolio` reads persisted portfolio state.
- `GET /api/alerts` reads persisted alerts.
- Paper-trade requests persist orders/fills/portfolio/position changes.
- Recommendation calls may persist recommendation history without changing the response contract.

## 12. Testing requirements

Persistence implementation requires:

- repository unit tests where business mapping is non-trivial;
- application tests proving persistence-backed paper execution;
- integration tests against PostgreSQL for transaction and concurrency invariants;
- migration validation;
- alert idempotency tests;
- restart/reload test proving state survives process restart.

Live Angel One credentials are not required for persistence tests.

## 13. Non-goals

This specification does not introduce:

- real-money trading;
- live order execution;
- advanced portfolio optimization;
- ML model persistence;
- feature-store architecture;
- distributed microservices;
- Kafka;
- mandatory TimescaleDB deployment;
- user authentication/authorization;
- tax accounting.

## 14. Acceptance criteria

1. A paper portfolio survives API/worker restart.
2. Positions and cash reload correctly from PostgreSQL.
3. Executed paper orders and fills are durable.
4. Portfolio/position/fill updates are atomic.
5. Concurrent execution cannot overspend cash or over-sell a position.
6. Risk alerts are durable and idempotent.
7. Market snapshots used by V1 workflows can be queried by symbol and time.
8. Recommendation history can be retrieved for audit/explanation.
9. No domain/application code references EF/PostgreSQL-specific types.
10. Migrations are source-controlled and testable.
11. CI remains independent of live broker credentials.
12. No real-money execution path is introduced.
