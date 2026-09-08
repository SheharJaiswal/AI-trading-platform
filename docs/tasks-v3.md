# V3 Trading Intelligence & Dashboard Delivery Checklist

## Spec gate

- [x] V3 trading intelligence and dashboard specification.
- [x] V3 scope preserves paper-only execution and deterministic risk authority.
- [x] V3 implementation branch created.

## V3.1 — Dashboard shell

- [x] Angular application shell.
- [x] Routing and responsive layout.
- [x] Typed API client.
- [x] Health/error/loading states.
- [x] Angular CI build/test configuration and dashboard state coverage.

## V3.2 — Portfolio workspace

- [x] Durable portfolio summary.
- [x] Positions table/detail.
- [x] P&L presentation.
- [x] Alert center.

## V3.3 — Research workspace

- [x] Symbol search/input.
- [x] Quote and freshness/provenance display.
- [x] Recommendation display.
- [x] Technical evidence display.
- [x] Explicit insufficient-data/stale-data states.

## V3.4 — AI research boundary

- [x] Structured AI research request/response contract.
- [x] Provider-neutral adapter.
- [ ] Cloud/local provider configuration boundary.
- [x] Explainable evidence and uncertainty presentation.
- [x] Verify AI cannot authorize execution.

## V3.5 — Paper-trade workflow

- [x] Explicit user confirmation.
- [x] Server-side deterministic risk decision.
- [x] Risk-blocked UI path.
- [x] Durable execution result display.
- [x] Audit-friendly status/error handling.

## Final gate

- [ ] .NET CI green on V3 branch.
- [ ] Angular CI green.
- [ ] API contract tests green.
- [ ] BA acceptance.
- [ ] Senior Engineer acceptance.
- [ ] Trader/safety acceptance.
- [ ] Explicit merge approval.
