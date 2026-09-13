# V30 Monitoring Run Status Hardening

## Objective

Keep durable monitoring-run outcome classification in one deterministic rule so the persisted status and returned result cannot drift from the validated monitoring counts.

## Scope

- Reuse `MonitoringRunStatusRules.Resolve` from `MonitoringRunService`.
- Preserve the existing completed, partially-failed and failed semantics.
- Preserve paper-only and observational monitoring boundaries.

## Acceptance criteria

- A run with no failures is `Completed`.
- A run with mixed successes/failures is `PartiallyFailed`.
- A run with all failures is `Failed`.
- Inconsistent monitoring counts remain rejected by the shared rule.
- Existing .NET and Docker CI pass at the exact PR head.
