# V10 Autonomous Paper Loop

## Goal
Implement the confirmed FR-10 sequence as a paper-only orchestration boundary:

Scan → Analyze → Predict → Rank → Recommend → Risk check → Paper order → Fill → Monitor.

## Safety boundaries
- Paper execution only; no brokerage integration.
- Deterministic risk remains authoritative.
- AI may advise but cannot authorize or bypass risk.
- No real-money execution.
- Every execution uses an idempotency key and durable paper-trading boundary.
- No order is created for HOLD, SELL, insufficient data, stale data, low confidence, or risk-blocked candidates.

## Delivery slices
1. Single-cycle orchestration contract.
2. Candidate ranking with deterministic tie-breaking.
3. Explicit no-decision outcomes.
4. Idempotent paper execution through the existing paper service.
5. Monitoring hand-off after successful fills.
6. Application tests for ordering, filtering, safety and idempotency.
7. Hosted/background scheduling only after the single-cycle behavior is proven.
