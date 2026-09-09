# V11 Monitoring Resilience

## Objective

Strengthen FR-11 monitoring so one market-data provider failure does not prevent other open paper positions from being refreshed. Monitoring failures remain observable through an injected sink without inventing trading thresholds or automatic position-closing rules.

## Safety boundary

- Paper-only monitoring.
- No automatic position closure is introduced.
- No new exposure or position-sizing rules are invented.
- Existing stop-loss alert semantics remain unchanged.
- Provider failures are isolated per position and reported to the monitoring failure sink.
