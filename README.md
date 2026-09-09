# AI Trading Platform

A spec-driven AI-assisted trading platform built around deterministic financial logic and provider abstractions.

## Capabilities

- .NET 10 API, application, domain and infrastructure layers
- MySQL durable persistence and Docker development environment
- Provider-neutral market data and historical OHLCV contracts
- Deterministic local historical CSV import/validation
- Technical indicators: SMA and RSI
- Candlestick pattern detection
- Baseline deterministic recommendation and risk engines
- Paper-only order execution with portfolio accounting
- Idempotent risk alerts through a background worker
- Provider-neutral AI gateway with disabled/local/cloud configuration
- Deterministic chronological backtesting with replay-prefix/no-lookahead enforcement
- Explicit fees, slippage, sizing and strategy-version assumptions
- Persisted backtest run configuration, result, simulated trades and risk ledger
- Angular research, paper-trading and backtesting workspaces
- OpenAPI, health endpoint and automated .NET + Angular CI

## Safety boundary

The platform is paper-only. Backtests never call broker execution and never mutate the live paper portfolio. AI output is advisory metadata only; deterministic risk checks remain authoritative. Backtest performance is simulation evidence, not a prediction of future returns.

## Run locally

```bash
docker compose up -d mysql
dotnet run --project src/AiTrading.Api
```

The default market provider is `demo`. MySQL persistence is enabled through the configured trading persistence connection.

## Key endpoints

- `GET /health`
- `GET /api/market/{symbol}/quote?instrumentToken=...`
- `GET /api/recommendations/{symbol}?instrumentToken=...`
- `POST /api/paper-trades/{symbol}?instrumentToken=...&quantity=...`
- `POST /api/ai/research`
- `GET /api/portfolio`
- `GET /api/alerts`
- `POST /api/backtests`
- `GET /api/backtests/{runId}`

## Backtesting workflow

1. Select symbol, interval and bounded historical dates.
2. Set starting cash, position size, fee and slippage assumptions.
3. Server validates historical data and provenance before replay.
4. Deterministic replay evaluates the shared strategy/risk path without lookahead.
5. The run persists its assumptions, result and simulated risk/trade ledger in MySQL.
6. Angular displays simulation status, provenance, metrics, equity curve and ledgers.

See `docs/specs`, `docs/plans` and `docs/tasks-v4.md` for the specification baseline and delivery checklist.