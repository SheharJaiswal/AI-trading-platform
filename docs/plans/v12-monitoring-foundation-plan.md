# V12 Monitoring Foundation Plan

This is the planning/acceptance slice for V12.1.

## Goal
Make each background-monitoring execution observable and restart-safe before adding market/news/portfolio detectors.

## BA decision
FR-11 is confirmed. Current coverage includes open-position refresh, stop-loss alerting, durable alerts, and provider-failure isolation. The next gap is run-level operational visibility.

## Senior-engineering decision
Implement monitoring-run lifecycle telemetry first. It provides a stable reliability boundary for later market, news and portfolio monitoring and avoids hiding partial failures.

## Trader/safety decision
Do not invent automatic liquidation, exposure limits, market-hours policy, suppression windows, or external notification channels. Monitoring remains advisory; deterministic risk remains authoritative.

## V12.1 acceptance

1. Every completed monitoring iteration has an observable outcome.
2. Partial provider failures are distinguishable from clean runs.
3. Cancellation is never reported as success.
4. Provider failure for one position does not stop unaffected positions.
5. Alert-delivery failure does not roll back durable alerts.
6. Restart/retry does not duplicate alerts or paper orders.
7. .NET and Docker CI remain green.

## Follow-on
V12.2 market-condition detectors; V12.3 news monitoring after provider selection; V12.4 portfolio-risk monitoring after concentration/drawdown policy confirmation.
