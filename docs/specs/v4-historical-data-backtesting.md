# V4 Historical Market Data & Backtesting Specification

## Goal

Turn the V3 trading workspace into a measurable research system by adding provider-neutral historical market data and a deterministic, paper-only backtesting engine. V4 must let a user replay historical data, evaluate the existing recommendation/risk logic, and inspect performance without creating live orders or treating AI output as execution authority.

## Scope

- Provider-neutral historical OHLCV candle contract with symbol, instrument identity, interval, timestamp, source and freshness/provenance metadata.
- Historical data ingestion/import boundary that can accept deterministic local datasets first and later support external providers without changing the domain contract.
- Durable historical candle storage in MySQL with uniqueness that prevents duplicate candles for the same instrument/interval/timestamp/source key.
- Deterministic backtest runner that replays candles chronologically and evaluates the existing recommendation and deterministic risk rules without calling live paper execution.
- Explicit backtest configuration: symbol/instrument, interval, start/end time, starting cash, position sizing and strategy version.
- Backtest result with equity curve, trades, realized/unrealized P&L, return, drawdown, win/loss counts and execution assumptions.
- Angular backtest workspace showing configuration, run status, summary metrics, equity curve and trade ledger.
- Clear separation between historical replay, deterministic strategy evaluation and optional AI research; AI remains advisory and cannot alter execution decisions.
- Contract and integration tests for chronology, duplicate handling, deterministic results, insufficient data and risk-blocked decisions.

## Non-goals

- Live brokerage execution.
- Autonomous trading.
- ML training or model optimization.
- AI-generated trade authorization.
- Short selling, leverage or derivatives.
- Real-time streaming infrastructure, Redis, RabbitMQ or microservice decomposition.
- Claims that backtest performance predicts future returns.

## Safety and correctness invariants

1. A backtest never calls a live broker order API and never mutates the live paper portfolio.
2. Historical candles are processed strictly by event time; future candles cannot influence an earlier decision.
3. The engine uses only information available at the replay timestamp, including indicators and recommendation inputs.
4. Risk rules remain deterministic and authoritative for simulated execution.
5. AI output, when enabled for research context, is advisory metadata only and cannot create or modify a simulated order.
6. Results are reproducible from the same dataset, configuration and strategy version.
7. Fees, slippage and fill assumptions are explicit configuration, not hidden defaults.
8. Missing, stale, duplicated or out-of-order historical data is surfaced rather than silently repaired into a misleading result.
9. Historical data provenance is retained with the dataset and exposed in the UI.
10. Backtest results are labeled as simulations and are never presented as live performance.

## User workflow

1. Select symbol/instrument and historical interval.
2. Select a bounded historical date range and starting cash.
3. Select strategy version and explicit fee/slippage assumptions.
4. Validate historical data completeness and provenance.
5. Run deterministic backtest.
6. Inspect equity curve, drawdown, trade ledger and risk-blocked decisions.
7. Compare results only across compatible datasets/configurations.
8. Export or persist the run configuration and result for auditability.

## Acceptance criteria

- Identical inputs produce identical backtest results.
- A duplicate historical candle cannot create duplicate replay events.
- A replay cannot access a candle later than the decision timestamp.
- Risk-blocked simulated trades are recorded with their risk decision and do not change cash or positions.
- The live paper portfolio is unchanged by any backtest.
- The UI clearly distinguishes historical simulation from paper execution.
- API contracts are typed and tested; no browser credentials are introduced.
- Incomplete data produces an explicit validation state and does not silently claim a valid result.

## Milestones

### V4.1 — Historical data contract and storage

Define candle contracts, provenance, MySQL persistence, import validation and duplicate protection.

### V4.2 — Deterministic replay engine

Build chronological replay and simulation accounting using existing recommendation and risk logic.

### V4.3 — Backtest API and audit model

Expose validated configuration, run lifecycle, result metrics and persisted run metadata.

### V4.4 — Backtesting workspace

Add Angular configuration, progress/error states, metrics, equity curve and trade ledger.

### V4.5 — Validation and acceptance

Run combined .NET/Angular CI and BA, Senior Engineer and Trader/Safety acceptance before any merge.

## Testing gate

CI must cover domain calculations, historical persistence, deterministic replay, data validation, risk-blocked simulation, API contracts and Angular backtest states. Integration tests must prove that backtests do not mutate the live paper portfolio.
