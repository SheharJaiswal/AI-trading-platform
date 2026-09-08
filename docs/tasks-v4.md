# V4 Historical Data & Backtesting Delivery Checklist

## Discovery gate

- [x] V4 scope selected: historical market data and deterministic backtesting.
- [x] Paper-only/live-trading safety boundary preserved.
- [x] AI remains advisory-only.
- [x] MySQL remains the durable store.

## V4.1 — Historical data

- [x] Typed OHLCV candle contract.
- [x] Provenance/source metadata.
- [x] MySQL migration and repository.
- [x] Duplicate and ordering validation.
- [ ] Deterministic local dataset import.

## V4.2 — Replay engine

- [ ] Chronological replay.
- [ ] No-lookahead enforcement.
- [ ] Reuse existing recommendation logic.
- [ ] Reuse deterministic risk gate.
- [ ] Explicit fee/slippage/fill assumptions.
- [ ] Deterministic simulation accounting.

## V4.3 — Backtest API/audit

- [ ] Run configuration contract.
- [ ] Run lifecycle/status.
- [ ] Persisted result and trade ledger.
- [ ] API validation/error states.
- [ ] Proof live paper portfolio is unchanged.

## V4.4 — Backtesting workspace

- [ ] Configuration form.
- [ ] Dataset validation/provenance state.
- [ ] Run status and errors.
- [ ] Equity curve and performance metrics.
- [ ] Simulated trade ledger.
- [ ] Explicit simulation-only labeling.

## Final gate

- [ ] .NET CI green.
- [ ] Angular CI green.
- [ ] API contract/integration tests green.
- [ ] BA acceptance.
- [ ] Senior Engineer acceptance.
- [ ] Trader/safety acceptance.
- [ ] Explicit merge approval.
