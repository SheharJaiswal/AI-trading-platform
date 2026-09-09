# V10 Autonomous Paper Loop

## Objective

Implement the first deterministic orchestration layer for FR-10 while preserving the platform's paper-only safety boundary.

## Cycle

`Scan → Analyze → Predict/score → Rank → Recommend → Risk check → Paper order → Fill → Monitor`

V10 exposes a single cycle operation for a **Running** paper session. The configured session symbols are scanned, recommendations are ranked deterministically, and at most the configured number of candidates is considered. Only a BUY candidate can reach the existing paper execution boundary.

## Safety invariants

- Execution mode is always `PAPER_ONLY`.
- Deterministic risk remains authoritative; the loop cannot bypass `IPaperTradeService`.
- A HOLD or NoDecision result never creates an order.
- Paused, stopped, or draft sessions cannot execute a cycle.
- Event IDs are deterministic per session/symbol/time bucket/interval, preserving idempotent paper execution.
- AI remains advisory-only and is not required to authorize an order.
- Live brokerage is not introduced.

## Configuration

- `AutonomousLoop:Quantity` — positive paper quantity per selected candidate (default `1`).
- `AutonomousLoop:MaxCandidates` — maximum ranked candidates considered for execution (default `1`).

This first increment intentionally provides a cycle endpoint rather than enabling an always-on scheduler. Scheduler cadence and market-hours rules remain explicit follow-up decisions rather than silently invented business rules.

## Endpoint

`POST /api/paper-sessions/{id}/run-cycle`

The endpoint requires MySQL persistence and a Running paper session. The response includes ranked candidates, the selected paper execution when approved, and an explicit no-decision reason otherwise.
