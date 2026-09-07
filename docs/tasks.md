# Implementation Task Breakdown

This checklist is derived from `docs/specs/v1-vertical-slice.md` and is ordered so each task leaves the repository in a buildable state.

## Spec gate

- [x] Discovery Spec Kit exists.
- [x] Vertical-slice specification approved for implementation.
- [x] Architecture/implementation plan written.
- [ ] Business/risk parameters that are intentionally open remain documented rather than invented.

## Milestone A — domain

- [ ] Create .NET 10 solution/projects.
- [ ] Add domain value objects and entities.
- [ ] Add market-data provider contracts.
- [ ] Add technical indicator calculations.
- [ ] Add candlestick detectors.
- [ ] Add recommendation contract and baseline strategy.
- [ ] Add deterministic risk gate.

## Milestone B — paper trading

- [ ] Add paper execution provider.
- [ ] Add portfolio/cash/position accounting.
- [ ] Add P&L calculations.
- [ ] Add order/fill audit records.
- [ ] Add unit tests for accounting invariants.

## Milestone C — market integration

- [ ] Add Angel One configuration/options.
- [ ] Add SmartAPI HTTP client.
- [ ] Add instrument master mapping.
- [ ] Add quote/candle mapping tests using recorded fixtures.
- [ ] Add provider failure/staleness handling.

## Milestone D — API and monitoring

- [ ] Add API endpoints and OpenAPI.
- [ ] Add portfolio/alert query DTOs.
- [ ] Add monitoring worker.
- [ ] Add idempotent stop-loss alert generation.
- [ ] Add worker tests with fake clock/provider.

## Milestone E — CI and review

- [ ] Add GitHub Actions build/test workflow.
- [ ] Verify no secrets or live execution paths are present.
- [ ] Review implementation against specification acceptance criteria.
- [ ] Fix review findings.
- [ ] Re-run CI.
- [ ] Request explicit merge approval before merging to `main`.

## Follow-on specifications after V1 slice

1. Persistent PostgreSQL/TimescaleDB model.
2. Fundamental data provider.
3. News provider and time-aware sentiment.
4. Prediction target/horizon and baseline ML model.
5. AI provider gateway with cloud + Ollama.
6. Autonomous opportunity loop.
7. Portfolio/risk parameterization.
8. Historical backtesting/evaluation.
9. Angular dashboard.
