# Background Monitoring Specification

## Objective

Continuously observe market, news, portfolio and model conditions and surface actionable risk changes.

## Monitoring inputs

- Latest prices
- Open positions
- Stop-loss distance/breach
- Target distance
- Sudden price movement
- Abnormal volume
- Technical trend changes
- Candlestick reversals
- Company/market news
- Material events
- Sentiment changes
- Portfolio drawdown
- Position/sector concentration
- Prediction/confidence changes

## Processing

```text
Scheduled/Event Trigger
        ↓
Fetch current data
        ↓
Normalize + validate
        ↓
Evaluate risk/events
        ↓
Create deduplicated alert
        ↓
Optional paper-exit workflow
```

## Reliability

Jobs must be restart-safe and idempotent. Retries must not duplicate paper orders or alerts.

## Scheduling

Monitoring frequency must be configurable and must respect provider rate limits.

## Alerts

Severity levels:

- INFO
- WARNING
- HIGH
- CRITICAL

Alert delivery must use an abstraction so UI, email, Telegram, push and other channels can be added later.

## Open decisions

- Exact monitoring cadence
- Event-driven vs polling mix
- Notification channels for V1
- Alert suppression windows
- Exact paper-exit automation policy
