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
- [ ] Angular CI build/test (CI build added; test remains pending).

## V3.2 — Portfolio workspace

- [ ] Durable portfolio summary.
- [ ] Positions table/detail.
- [ ] P&L presentation.
- [ ] Alert center.

## V3.3 — Research workspace

- [ ] Symbol search/input.
- [ ] Quote and freshness/provenance display.
- [ ] Recommendation display.
- [ ] Technical evidence display.
- [ ] Explicit insufficient-data/stale-data states.

## V3.4 — AI research boundary

- [ ] Structured AI research request/response contract.
- [ ] Provider-neutral adapter.
- [ ] Cloud/local provider configuration boundary.
- [ ] Explainable evidence and uncertainty presentation.
- [ ] Verify AI cannot authorize execution.

## V3.5 — Paper-trade workflow

- [ ] Explicit user confirmation.
- [ ] Server-side deterministic risk decision.
- [ ] Risk-blocked UI path.
- [ ] Durable execution result display.
- [ ] Audit-friendly status/error handling.

## Final gate

- [ ] .NET CI green on V3 branch.
- [ ] Angular CI green.
- [ ] API contract tests green.
- [ ] BA acceptance.
- [ ] Senior Engineer acceptance.
- [ ] Trader/safety acceptance.
- [ ] Explicit merge approval.
