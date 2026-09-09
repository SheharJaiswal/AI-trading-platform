# V10 Autonomous Paper Loop

FR-10 implementation plan: Scan → Analyze → Predict → Rank → Recommend → Risk check → Paper order → Fill → Monitor.

Safety: paper-only, deterministic risk authoritative, AI advisory-only, idempotent execution, explicit no-decision outcomes, and no live brokerage.

Delivery slices: single-cycle orchestration, deterministic ranking, no-decision states, idempotent execution, monitoring hand-off, tests, then background scheduling.
