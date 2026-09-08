# V3 Trading Intelligence & Dashboard Implementation Plan

## Delivery order

1. Create Angular workspace and establish API environment/configuration.
2. Implement shared typed API models/services for health, portfolio, alerts, quotes, recommendations and paper trades.
3. Build dashboard shell and navigation.
4. Build portfolio/positions/alerts workspace.
5. Build research workspace with freshness/provenance and explicit no-decision states.
6. Define structured AI research contract and provider adapter.
7. Integrate explicit paper-trade confirmation and risk-result workflow.
8. Add Angular unit/component tests and API contract tests.
9. Run combined .NET + Angular CI.
10. Perform BA, Senior Engineer and Trader/Safety acceptance before merge.

## Engineering rules

- Reuse V2 durable APIs; do not duplicate persistence logic in Angular.
- Keep AI advisory-only.
- Never move risk authorization into the browser.
- Keep credentials out of frontend source and static configuration.
- Prefer a modular monolith over new services unless measurable need appears.
- Make data freshness and provenance visible rather than implying real-time certainty.
