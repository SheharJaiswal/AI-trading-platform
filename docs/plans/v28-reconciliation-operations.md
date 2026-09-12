# V28 — Recovery reconciliation operations

## Scope
Expose the existing deterministic durable-short recovery reconciliation as a read-only application operation.

## Boundary
The operation is diagnostic only. It does not mutate positions, retry orders, liquidate positions, call a broker, or bypass deterministic risk controls.

## Acceptance
- deterministic output
- explicit inconsistency reasons
- paper-only behavior
- focused application tests
- exact-head .NET and Docker CI before merge
