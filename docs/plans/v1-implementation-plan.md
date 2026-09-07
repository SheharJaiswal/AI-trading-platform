# V1 Implementation Plan

Status: READY TO IMPLEMENT

## Architecture

Use a modular ASP.NET Core/.NET 10 solution rather than microservices for the first slice.

```text
src/
  AiTrading.Api/              HTTP endpoints and composition root
  AiTrading.Application/      use cases and orchestration
  AiTrading.Domain/           trading domain, value objects, rules
  AiTrading.Infrastructure/   Angel One adapter, clocks, external integrations
  AiTrading.Worker/           background monitoring host

tests/
  AiTrading.Domain.Tests/
  AiTrading.Application.Tests/
  AiTrading.Infrastructure.Tests/
```

The solution may later add Python services, PostgreSQL/TimescaleDB, Redis, RabbitMQ, and Angular without changing domain ownership.

## Dependency direction

```text
Api ───────────────► Application ───────────────► Domain
 │                       │
 └──────────────────────► Infrastructure ───────► Domain
Worker ────────────────► Application
```

Domain must not depend on ASP.NET, Angel One SDK/DTOs, database libraries, or LLM SDKs.

## Implementation sequence

### Task 1 — Bootstrap
- Create .NET 10 solution and projects.
- Enable nullable reference types and warnings-as-errors where practical.
- Add central package/version configuration only when packages are actually required.
- Add xUnit tests.

### Task 2 — Domain contracts
- Symbol/instrument identity.
- OHLCV candle and normalized quote.
- Technical indicators.
- Candlestick pattern result.
- Recommendation and risk decision.
- Paper order, fill, position, portfolio, alert.

### Task 3 — Technical/candlestick engine
- Pure functions.
- Deterministic decimal/double policy documented.
- Fixture-based tests for known candle sequences.

### Task 4 — Recommendation and risk
- Baseline deterministic recommendation strategy.
- Risk gate before execution.
- Explicit `NO_DECISION`, `RISK_BLOCKED`, and `INSUFFICIENT_DATA` outcomes.

### Task 5 — Paper execution
- In-memory portfolio for the first slice.
- Buy-only execution.
- Cash and position invariants.
- Realized/unrealized P&L.

### Task 6 — Angel One market adapter

Use the official SmartAPI endpoints currently documented by Angel One:
- Live quote: `/rest/secure/angelbroking/market/v1/quote/`
- Historical candles: `/rest/secure/angelbroking/historical/v1/getCandleData`
- Instrument master: `https://margincalculator.angelone.in/OpenAPI_File/files/OpenAPIScripMaster.json`

The adapter must isolate authentication/header construction, HTTP transport, provider response validation, and mapping. Credentials are configuration/secret inputs only.

For the first slice, market-data calls are allowed; execution calls are not implemented or invoked.

### Task 7 — API

Minimum endpoints:
- `GET /api/market/{symbol}/quote`
- `GET /api/recommendations/{symbol}`
- `GET /api/portfolio`
- `GET /api/alerts`

Use OpenAPI and stable DTOs. Do not expose domain persistence models directly.

### Task 8 — Background monitoring
- Hosted service with configurable interval.
- Evaluate open positions.
- Detect stop-loss breach.
- Deduplicate alert creation.
- Never place an order from the monitor.

### Task 9 — CI

GitHub Actions must run:
- restore
- build
- unit tests
- formatting/static checks if configured

No integration test should require live broker credentials.

## Definition of done for this plan

The first slice is complete only when the application builds, automated tests pass, the provider boundary is proven with adapter tests, the paper portfolio is observable through API, and the monitor is proven idempotent.

After that PR is reviewed and explicitly approved, merge it. Only then start the next feature specification for persistence and richer analysis.
