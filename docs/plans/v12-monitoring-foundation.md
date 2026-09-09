# V12 Monitoring Foundation

## V12.1 implementation

The first implementation slice closes the safe operational part of FR-11 by recording every durable monitoring execution as a run with a lifecycle outcome.

Implemented:

- `Running`, `Completed`, `PartiallyFailed`, and `Failed` run states.
- Durable start/completion timestamps.
- Position and provider-failure counts.
- Per-position provider failures remain isolated.
- Cancellation propagates instead of being reported as success.
- Alert delivery remains advisory after durable commit.
- No new trading thresholds, liquidation rules, notification channels, or market-hours assumptions.

## Acceptance gate

The slice is complete only after .NET tests, MySQL integration tests, and Docker image/Compose validation are green.

## Follow-on

Market-condition, news, and portfolio-risk detectors remain separate slices and must use explicitly configured rules/contracts before implementation.
