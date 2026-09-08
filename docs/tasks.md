# Implementation Task Breakdown

This checklist is the living delivery plan for the approved V1 vertical slice. Business/risk parameters that are intentionally open remain documented rather than invented.

## Spec gate

- [x] Discovery Spec Kit exists.
- [x] Vertical-slice specification approved for implementation.
- [x] Architecture/implementation plan written.
- [x] Open business/risk parameters documented.

## Milestone A — domain

- [x] Create .NET 10 solution/projects.
- [x] Add domain value objects and entities.
- [x] Add market-data provider contracts.
- [x] Add technical indicator calculations.
- [x] Add candlestick detectors.
- [x] Add recommendation contract and baseline strategy.
- [x] Add deterministic risk gate.

## Milestone B — paper trading

- [x] Add paper execution provider.
- [x] Add portfolio/cash/position accounting.
- [x] Add realized P&L calculations.
- [x] Add order/fill audit domain records.
- [x] Add unit tests for accounting invariants.
- [x] Verify blocked recommendations never reach execution.

## Milestone C — market integration

- [x] Add Angel One configuration/options.
- [x] Add SmartAPI HTTP client.
- [x] Add normalized quote/candle mapping.
- [x] Add deterministic demo provider for development/testing.
- [x] Add recorded Angel One fixture tests.
- [x] Add provider failure validation and required-token handling.
- [x] Preserve provider exchange timestamp as market-data provenance.
- [x] Prevent stale/invalid market data from becoming executable.

## Milestone D — API and monitoring

- [x] Add API endpoints and OpenAPI.
- [x] Add portfolio/alert query endpoints.
- [x] Add monitoring worker.
- [x] Add idempotent stop-loss alert generation.
- [x] Use position/rule/evaluation-minute bucket for stop-loss alert identity.
- [x] Add worker/monitoring tests.
- [x] Add health endpoint.
- [x] Verify stable API JSON contracts.
- [x] Add correlation context to worker error logs without credentials.

## Milestone E — CI and review

- [x] Add GitHub Actions build/test workflow.
- [x] Consolidate duplicate build/test workflows.
- [x] Verify no secrets are committed and live execution is unavailable.
- [x] Add provider-neutral AI gateway boundary.
- [ ] Run CI successfully against the final review branch commit.
- [ ] Review implementation against all specification acceptance criteria after CI is green.
- [ ] Fix any remaining review findings.
- [ ] Request explicit merge approval before merging feature work to `main`.

## Follow-on specifications

1. MySQL persistence implementation with migrations/repositories.
2. Angular dashboard.
3. Fundamental data provider.
4. News provider and time-aware sentiment.
5. Prediction target/horizon and baseline ML model.
6. AI provider gateway with cloud AI + Ollama implementations.
7. Autonomous opportunity loop.
8. Portfolio/risk parameterization.
9. Historical backtesting/evaluation.
10. Redis/RabbitMQ event infrastructure when justified by load and workflow requirements.
