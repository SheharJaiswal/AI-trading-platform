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
- Idempotent risk alerts through a restart-safe background monitor
- Provider-neutral AI gateway with disabled/local/cloud configuration
- Deterministic chronological backtesting with replay-prefix/no-lookahead enforcement
- Explicit fees, slippage, sizing and strategy-version assumptions
- Persisted backtest run configuration, result, simulated trades and risk ledger
- Prediction/outcome recording and deterministic evaluation metrics
- Deterministic autonomous paper-session cycle with ranked candidates and no-decision outcomes
- Angular research, paper-trading and backtesting workspaces
- OpenAPI, health endpoint and automated .NET + Angular CI

## Safety boundary

The platform is paper-only. Backtests never call broker execution and never mutate the live paper portfolio. AI output is advisory metadata only; deterministic risk checks remain authoritative. Backtest performance is simulation evidence, not a prediction of future returns.

## Run locally

### Full application with Docker Compose

The complete stack runs as three containers: MySQL, the .NET API, and the Angular/Nginx web application. The API applies the committed EF Core migrations before starting, and the web container reverse-proxies `/api` and `/health` to the API.

```bash
docker compose up --build
```

Open the dashboard at `http://localhost:4200` and the API health endpoint at `http://localhost:8080/health`.

To stop the stack while retaining MySQL data:

```bash
docker compose down
```

To remove the persisted MySQL volume as well:

```bash
docker compose down -v
```

### API only

```bash
docker compose up -d mysql
dotnet run --project src/AiTrading.Api
```

The default market provider is `demo`. Docker Compose enables MySQL persistence, uses a local demo market-data provider, and keeps AI disabled by default. Provider credentials are not stored in the Compose file.

### Background risk monitoring

When MySQL persistence is enabled, the API starts a restart-safe background risk monitor. It checks open positions, refreshes market prices, stores market-data snapshots, and raises deduplicated stop-loss alerts. The monitor does not submit live or broker orders and does not automatically close positions in this delivery slice.

Configure the cadence with `Monitoring:RiskIntervalSeconds`; the Docker Compose default is 60 seconds.

### Autonomous paper cycle

A running paper session can execute one deterministic autonomous cycle through `POST /api/paper-sessions/{id}/run-cycle`. The cycle scans configured symbols, ranks recommendations deterministically, and only sends a BUY candidate through the existing paper execution and risk boundary. HOLD/NoDecision candidates do not create orders.

Configure `AutonomousLoop:Quantity` and `AutonomousLoop:MaxCandidates`. The Compose defaults are 1 for both. The cycle is deliberately an explicit operation in this delivery slice; always-on scheduling and market-hours rules remain separate business decisions.

## Key endpoints

- `GET /health`
- `GET /api/market/{symbol}/quote?instrumentToken=...`
- `GET /api/recommendations/{symbol}?instrumentToken=...`
- `POST /api/paper-trades/{symbol}?instrumentToken=...&quantity=...`
- `POST /api/paper-sessions/{id}/run-cycle`
- `POST /api/ai/research`
- `GET /api/portfolio`
- `GET /api/alerts`
- `POST /api/backtests`
- `GET /api/backtests/{runId}`
- `POST /api/evaluations/predictions`
- `POST /api/evaluations/outcomes`
- `GET /api/evaluations`
- `GET /api/evaluations/metrics`

## Backtesting workflow

1. Select symbol, interval and bounded historical dates.
2. Set starting cash, position size, fee and slippage assumptions.
3. Server validates historical data and provenance before replay.
4. Deterministic replay evaluates the shared strategy/risk path without lookahead.
5. The run persists its assumptions, result and simulated risk/trade ledger in MySQL.
6. Angular displays simulation status, provenance, metrics, equity curve and ledgers.

See `docs/specs`, `docs/plans` and `docs/tasks-v4.md` for the specification baseline and delivery checklist.