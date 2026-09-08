# V4 Historical Market Data & Backtesting Implementation Plan

## Delivery order

1. Define a provider-neutral OHLCV candle model with instrument, interval, event time and provenance.
2. Add MySQL historical-candle schema, migration and repository contract with duplicate protection.
3. Add deterministic local dataset import and validation for gaps, duplicates, ordering and provenance.
4. Extract/reuse strategy evaluation so replay consumes the same deterministic recommendation and risk rules as the live paper workflow.
5. Implement an isolated backtest simulator with explicit starting cash, sizing, fees, slippage and chronological fills.
6. Persist backtest configuration, run metadata, simulated orders/trades and aggregate results without touching the live paper portfolio.
7. Add API contracts for data validation, backtest submission/status/result and trade ledger.
8. Build Angular backtest workspace with explicit validation, running, completed, insufficient-data and error states.
9. Add deterministic unit/integration/API/Angular tests, including a proof that backtests leave the live portfolio unchanged.
10. Run combined .NET + Angular CI and perform BA, Senior Engineer and Trader/Safety acceptance.

## Engineering rules

- Reuse existing domain recommendation and deterministic risk logic; do not create a second strategy implementation for replay.
- Keep backtesting isolated from live/paper execution and broker adapters.
- Keep AI advisory-only; AI cannot change simulated order authorization.
- Treat event time and provenance as first-class data; never use future information.
- Make fees and slippage explicit and versioned with the run configuration.
- Prefer deterministic local datasets for the first implementation; external historical providers remain adapters behind the contract.
- Do not introduce Redis, RabbitMQ or microservices for V4.
- Preserve MySQL as the durable store and keep credentials out of the browser.
