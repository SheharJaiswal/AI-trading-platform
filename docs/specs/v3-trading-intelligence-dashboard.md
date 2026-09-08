# V3 Trading Intelligence & Dashboard Specification

## Goal

Deliver the first usable Angular trading workspace on top of the V1/V2 APIs, while establishing the provider-neutral AI research boundary for explainable paper-trading decisions.

## Scope

- Angular dashboard for portfolio, positions, alerts, market quote, recommendation and paper-trade visibility.
- Stock research workspace combining current quote, technical recommendation and AI research output.
- Structured recommendation presentation: action, confidence/probability, horizon, thesis, invalidation, risks, evidence and timestamp/provenance.
- AI research remains advisory and cannot authorize or execute an order.
- Paper trading remains the only execution mode; deterministic risk controls remain authoritative.
- Provider-neutral AI configuration boundary suitable for cloud and local providers.
- API contract models required by the dashboard.

## Non-goals

- Live brokerage execution.
- Autonomous trading loop.
- ML model training or production prediction model.
- News/fundamental provider implementation in this milestone.
- Redis/RabbitMQ or microservice decomposition.

## Dashboard requirements

1. Home/portfolio view shows cash, positions, unrealized/realized P&L and recent alerts.
2. Research view accepts a symbol and shows quote plus recommendation.
3. Research view exposes AI research separately from deterministic recommendation/risk output.
4. Paper-trade action requires explicit user initiation and displays risk decision before submission.
5. Position detail shows entry price, current price, quantity, stop loss and alerts.
6. Every market/recommendation/research result displays freshness/provenance where available.
7. Loading, stale-data, insufficient-data, risk-blocked and API-error states are explicit.
8. No UI action can bypass the server-side risk gate.

## Acceptance invariants

- AI output is never treated as an execution authorization.
- A blocked risk decision cannot be submitted as an approved paper trade.
- UI displays paper-only execution clearly.
- API contracts are stable and tested.
- No credentials or secrets are stored in browser source/configuration.

## Milestones

### V3.1 Dashboard shell

Angular application, routing, typed API client, responsive layout, health/error handling.

### V3.2 Portfolio workspace

Portfolio/positions/alerts views using durable V2 APIs.

### V3.3 Research workspace

Quote, recommendation, technical evidence and explicit data freshness/provenance.

### V3.4 AI research boundary

Structured research request/result contracts and configurable provider adapter without execution authority.

### V3.5 Paper-trade workflow

Explicit confirmation, server-side risk result, durable execution result and audit-friendly status.

## Testing gate

CI must validate Angular build/test and existing .NET restore/build/test. API contract tests must cover dashboard-facing response shapes and risk-blocked paper-trade behavior.
