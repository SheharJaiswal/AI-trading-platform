# V7 Continuous Position Monitoring

## Goal

Close the remaining position-monitoring gap in FR-11 by refreshing market state for every open paper position, while retaining deterministic stop-loss alerting.

## Delivery

1. Query every open position on each monitoring iteration, not only positions that already have a stop loss.
2. Refresh the position's current market price and updated timestamp.
3. Persist a market-data snapshot for each refreshed quote.
4. Evaluate stop-loss alerts only when a stop-loss is configured.
5. Preserve existing alert deduplication and paper-only boundaries.

## Safety

- Monitoring never creates live broker orders.
- Monitoring never auto-closes positions in this slice.
- Risk rules remain authoritative for execution.
- Missing stop-loss configuration does not disable market-price monitoring.

## Acceptance

- Existing CI remains green.
- Every open paper position receives a market refresh on each iteration.
- Stop-loss alert behavior remains idempotent.
- No live execution path is introduced.
