# V5 Paper Trading Operations Implementation Plan

## Agent convergence

**BA:** V5 should make paper trading operationally observable and repeatable, building directly on V4's deterministic research baseline.

**Senior Engineer:** Reuse the existing recommendation, deterministic risk and paper-execution boundaries. Add session orchestration and durable audit state without introducing new infrastructure.

**Trader/Safety:** Preserve paper-only execution. Idempotency, explicit risk authority, lifecycle controls and auditability are mandatory before operational use.

**Developer:** Deliver in four milestones, each CI-gated, with no live broker integration.

## Delivery order

1. Define session lifecycle and immutable configuration contracts.
2. Add MySQL persistence for sessions and operational audit entities.
3. Implement lifecycle API and validation.
4. Integrate market-event processing with existing deterministic recommendation/risk and paper execution boundaries.
5. Add idempotency and duplicate-event protection.
6. Add session monitoring and audit APIs.
7. Build Angular operations workspace.
8. Add integration/UI/safety tests.
9. Run combined CI and complete acceptance review.

## Engineering rules

- Reuse existing deterministic recommendation and risk logic.
- Reuse the existing paper execution boundary; do not create a second order executor.
- Never add a live broker execution path as part of V5.
- Keep AI advisory-only.
- Keep MySQL as durable storage.
- Do not add Redis, RabbitMQ or microservices.
- Persist enough state to reconstruct paper-session decisions and portfolio changes.
- Make lifecycle transitions explicit and reject invalid transitions.
