# V4 Backtesting Architecture Decisions

## Decision 1 — Backtest isolation

Backtesting is a simulation boundary, not a paper-execution shortcut. The simulator receives strategy/risk decisions and market candles but has no dependency on the live paper execution provider or live portfolio mutation path.

## Decision 2 — One deterministic strategy path

The backtest should reuse the existing recommendation and risk logic. Divergent replay-only trading rules would make performance results difficult to trust and maintain.

## Decision 3 — Historical data first

V4 begins with a deterministic local historical dataset/import path. Provider adapters may be added later behind the same historical-data contract.

## Decision 4 — Explicit simulation assumptions

Fees, slippage, fill model, starting cash, sizing and strategy version are persisted as part of each run. Results without those assumptions are not considered auditable.

## Decision 5 — AI boundary

AI research may annotate a run or help explain results, but AI cannot authorize, modify or inject simulated orders. The deterministic risk engine remains authoritative.

## Decision 6 — No-lookahead as a hard invariant

A strategy evaluation at timestamp T may consume only data available at or before T. Indicators and rolling windows must be computed from the replay prefix, never from the complete future dataset.
