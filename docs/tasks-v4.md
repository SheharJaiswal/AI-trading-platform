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
- [x] Deterministic local CSV dataset import/parser with strict schema and invariant parsing.
- [x] Stable historical candle identifiers and null-safe persistence identity.

## V4.2 — Replay engine
- [x] Chronological replay.
- [x] No-lookahead enforcement through replay-prefix evaluation.
- [x] Reuse existing recommendation logic through shared deterministic evaluator.
- [x] Reuse deterministic risk gate.
- [x] Explicit fee/slippage/fill assumptions.
- [x] Deterministic simulation accounting and equity curve.
- [x] Risk decisions captured as immutable replay events.
- [x] Replay engine has no portfolio/execution-provider dependency.
- [ ] Integration proof that live paper portfolio remains unchanged.

## V4.3 — Backtest API/audit
- [x] Run configuration contract.
- [x] Synchronous completed run lifecycle/status.
- [x] Historical data retrieval and result contract.
- [x] API validation/error states.
- [ ] Persisted result and trade ledger.
- [ ] Integration proof live paper portfolio is unchanged.

## V4.4 — Backtesting workspace
- [x] Configuration form.
- [ ] Dataset validation/provenance state.
- [ ] Run status and errors from API.
- [ ] Equity curve and performance metrics.
- [ ] Simulated trade ledger.
- [x] Explicit simulation-only labeling.

## Final gate
- [ ] .NET CI green.
- [ ] Angular CI green.
- [ ] API contract/integration tests green.
- [ ] BA acceptance.
- [ ] Senior Engineer acceptance.
- [ ] Trader/safety acceptance.
- [ ] Explicit merge approval.
