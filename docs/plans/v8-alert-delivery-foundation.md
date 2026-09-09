# V8 Alert Delivery Foundation

## BA objective

Advance FR-12 without inventing unresolved notification-channel or suppression-window business rules. Alerts must remain visible through the existing API while delivery is isolated behind an abstraction for future UI, email, Telegram, push, or other channels.

## Engineering scope

- Introduce an application-level alert delivery abstraction.
- Provide a safe default implementation that does not send external notifications.
- Keep durable alert persistence and existing deduplication as the source of truth.
- Invoke delivery only after an alert is accepted by the durable alert store.
- Preserve restart safety and avoid duplicate external delivery attempts for alerts rejected as duplicates.
- Keep paper-only behavior; no broker/live execution and no automatic position closure.

## Safety constraints

- Alert severity remains INFO/WARNING/HIGH/CRITICAL.
- No notification channel is enabled by default because V1 channel selection remains an open discovery decision.
- No suppression policy is invented in this slice; existing alert deduplication remains authoritative.
- Delivery failures must not prevent the monitoring loop from continuing.

## Acceptance

1. Alert delivery is represented by an injectable application abstraction.
2. Durable monitoring delivers only newly persisted alerts.
3. Default delivery has no external side effects.
4. Delivery exceptions are isolated from monitoring iteration failures.
5. Existing stop-loss deduplication remains unchanged.
6. .NET and Docker CI remain green.
