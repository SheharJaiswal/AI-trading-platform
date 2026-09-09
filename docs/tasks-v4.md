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
- [x] Invalid ranges/configurations are rejected before persistence access.
- [x] Structural safety proof that live paper portfolio is outside replay dependencies.

## V4.3 — Backtest API/audit
- [x] Run configuration contract.
- [x] Synchronous completed run lifecycle/status.
- [x] Historical data retrieval and result contract.
- [x] API validation/error states.
- [x] Persisted result and trade/risk ledger.
- [x] Persisted run configuration and strategy version.
- [x] Backtest result retrieval by run ID.
- [x] Live paper portfolio remains outside the backtest service dependency graph.

## V4.4 — Backtesting workspace
- [x] Configuration form.
- [x] API client and typed request/result models.
- [x] Run status and errors from API.
- [x] Equity/performance metrics summary.
- [x] Simulated trade ledger.
- [x] Dataset validation/provenance state.
- [x] Risk decision ledger.
- [x] Explicit simulation-only labeling.

## V4.5 — Validation and acceptance
- [x] Combined .NET + Angular CI acceptance gate configured.
- [x] Deterministic replay tests.
- [x] Historical validation tests.
- [x] Backtest service validation tests.
- [x] Angular backtest workspace tests.
- [x] Safety test proving replay has no live portfolio/execution dependency.
- [ ] Final CI run green after the final implementation commit.
- [ ] BA acceptance.
- [ ] Senior Engineer acceptance.
- [ ] Trader/safety acceptance.
- [ ] Explicit merge approval.

Implementation is complete; these final boxes are release-gate evidence, not unfinished V4 product functionality.