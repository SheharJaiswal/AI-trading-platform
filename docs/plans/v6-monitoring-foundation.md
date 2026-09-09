# V6 Monitoring Foundation

## Goal

Turn the existing durable stop-loss checker into a restart-safe background monitoring capability while preserving paper-only execution boundaries.

## Delivery slice

1. Run durable risk checks on a configurable interval.
2. Create a fresh dependency-injection scope for every iteration.
3. Persist refreshed position market prices and market-data snapshots.
4. Deduplicate stop-loss alerts using the existing alert store contract.
5. Continue monitoring after an individual iteration failure.
6. Keep the monitor disabled when MySQL persistence is disabled.
7. Keep AI advisory-only and do not introduce automatic live execution.

## Safety rules

- Monitoring may observe and alert; it must not create broker/live orders.
- Stop-loss monitoring does not automatically close a position in this slice.
- Each iteration is isolated by a DI scope so DbContext/unit-of-work state is not reused across cycles.
- Configuration is explicit through `Monitoring:RiskIntervalSeconds` and must be positive.
- Exceptions are logged and do not terminate the monitoring process.

## Acceptance

- Application builds and tests cleanly.
- Docker Compose validation remains green.
- Docker image build remains green.
- Existing paper-trading and backtest tests remain green.
- Monitoring remains restart-safe and idempotent at the alert boundary.
