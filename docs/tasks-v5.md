# V5 Paper Trading Operations Tasks

## V5.1 — Session lifecycle
- [x] Session configuration contract
- [x] Draft/running/paused/stopped state machine
- [x] Invalid transition protection
- [x] MySQL session repository and migration
- [x] Session lifecycle API
- [x] Lifecycle unit tests

## V5.2 — Operational paper execution
- [x] Session event contract
- [x] Running-session guard
- [x] Configured-symbol guard
- [x] Deterministic session-event idempotency key
- [x] Reuse existing durable paper execution/risk boundary
- [x] Session event API
- [x] Durable event/risk audit persistence
- [x] Session audit API for events, orders and fills

## V5.3 — Operations workspace
- [x] Paper session configuration UI
- [x] Lifecycle controls
- [x] Market-event processing control
- [x] Explicit paper-only safety labeling
- [x] Order/fill/risk history API foundation
- [x] Portfolio/equity query foundation via durable portfolio API
- [ ] Order/fill/risk history dashboard expansion
- [ ] Portfolio/equity operational snapshots UI

## V5.4 — Acceptance
- [ ] .NET + Angular CI green on final V5 head
- [ ] Integration tests for durable session persistence
- [ ] API tests for lifecycle/event idempotency
- [ ] Angular component tests
- [ ] BA acceptance
- [ ] Senior Engineer acceptance
- [ ] Trader/Safety acceptance
- [ ] Merge V5 to main
