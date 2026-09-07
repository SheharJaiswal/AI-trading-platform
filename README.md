# AI Trading Platform

A spec-driven AI-assisted trading platform built around deterministic financial logic and provider abstractions.

## V1 capabilities

- .NET 10 API, application, domain and infrastructure layers
- MySQL schema and Docker development environment
- Provider-neutral market data contract
- Angel One market-data adapter
- Deterministic demo market-data provider for local development
- Technical indicators: SMA and RSI
- Candlestick pattern detection
- Baseline recommendation engine
- Deterministic risk gate
- Paper-only order execution
- Portfolio and realized P&L accounting
- Idempotent stop-loss alerts through a background worker
- Provider-neutral AI gateway contract
- OpenAPI and health endpoint
- Automated build/test CI

## Safety boundary

V1 cannot place live broker orders. AI output cannot authorize or execute trades, and risk checks remain deterministic.

## Run locally

```bash
docker compose up -d mysql
dotnet run --project src/AiTrading.Api
```

The default market provider is `demo`, so broker credentials are not required for local development.

Set `MarketData:Provider` to `angelone` and supply credentials through environment variables or user secrets only when intentionally testing the broker market-data adapter.

## Key endpoints

- `GET /health`
- `GET /api/market/{symbol}/quote?instrumentToken=...`
- `GET /api/recommendations/{symbol}?instrumentToken=...`
- `POST /api/paper-trades/{symbol}?instrumentToken=...&quantity=...`
- `POST /api/ai/research`
- `GET /api/portfolio`
- `GET /api/alerts`

## Architecture

```text
Angular + TypeScript
        |
ASP.NET Core / .NET 10
        |
Application + Domain + Infrastructure
        |
      MySQL
        |
 .NET Worker
        |
AI Gateway / Python ML services (follow-on)
```

See `docs/discovery` and `docs/specs` for the requirements and specification baseline.
