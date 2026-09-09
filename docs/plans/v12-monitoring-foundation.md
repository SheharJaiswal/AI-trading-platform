# V12 Monitoring Foundation Plan

## Objective

Close the next safe slice of FR-11 by making background monitoring observable and restart-safe without inventing unresolved trading or notification policy.

## Agent review

### BA

FR-11 is confirmed and requires continuous monitoring of open positions, market conditions, news and portfolio risk. The current implementation has durable position price refresh and stop-loss alerts, plus provider-failure isolation. The remaining confirmed gap is broader monitoring coverage and operational visibility.

### Senior Engineer

The next implementation slice should add explicit monitoring-run state/telemetry before adding more event rules. This avoids hidden failures, makes retries diagnosable, and keeps monitoring concerns separate from paper execution.

### Trader/Safety

Do not introduce automatic liquidation, new exposure thresholds, market-hours assumptions, alert suppression windows, or external notification channels while those decisions remain open. Monitoring may observe and alert; deterministic risk remains authoritative.

## V12.1 scope

- Record a monitoring run outcome for each scheduled execution.
- Distinguish completed, partially failed and failed runs.
- Preserve provider-failure details without stopping unaffected positions.
- Keep cancellation semantics explicit.
- Keep alert delivery advisory and non-blocking after durable commit.
- Add integration coverage for run-level observability.

## Explicit non-goals

- Live broker execution.
- Automatic paper exits.
- New risk thresholds.
- News-provider integration without a confirmed provider contract.
- Fundamental-data integration without a confirmed provider contract.
- External notification channels.
- A fixed monitoring cadence beyond the existing configurable setting.

## Acceptance criteria

1. A monitoring iteration has an observable lifecycle outcome.
2. Partial provider failures are distinguishable from a clean run.
3. Cancellation is not recorded as a successful run.
4. One provider failure cannot prevent unaffected positions from being processed.
5. Alert-delivery failure cannot roll back a durable alert.
6. Re-running after restart does not create duplicate alerts or paper orders.
7. Combined .NET and Docker CI remains green.

## Follow-on slices

- V12.2 market-condition detectors using explicitly configured thresholds.
- V12.3 news monitoring once the confirmed news-provider contract is selected.
- V12.4 portfolio-risk monitoring once concentration/drawdown policies are confirmed.
