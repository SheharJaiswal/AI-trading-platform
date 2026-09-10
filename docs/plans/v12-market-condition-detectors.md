# V12.2 Market Condition Detectors

## BA decision

Implement the confirmed FR-11 market-condition observation slice without inventing trading policy. Detection thresholds are explicit configuration, not hard-coded business decisions.

## Implemented

- Sudden price-move detection against the latest strictly prior candle.
- Abnormal-volume detection against the recent prior-volume average.
- Warning/High severity classification for observed events.
- Structured event output for later durable alert integration.
- Future-data exclusion to preserve causal monitoring semantics.

## Safety

Detectors observe and report only. They cannot place, modify, or close paper orders. Deterministic risk remains authoritative.

## Non-goals

- Automatic liquidation.
- Fixed market-hours assumptions.
- News-provider selection.
- Portfolio concentration/drawdown policy.
- External notification channels.

## Acceptance

- Tests cover price movement, abnormal volume, future-data exclusion, and invalid configuration.
- Integration into the durable monitor should happen only after the detector contract is reviewed against existing alert deduplication semantics.
