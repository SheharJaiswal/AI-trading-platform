# Non-Functional Requirements

## Reliability

- Background jobs must be restart-safe and idempotent.
- Provider outages must fail gracefully.
- Duplicate orders and alerts must be prevented.
- Critical domain operations must be transactional where appropriate.

## Data integrity

- Financial calculations must use deterministic logic.
- Market timestamps must be preserved.
- Internal timestamps use UTC.
- External data must be validated before entering domain workflows.
- Historical inputs used for evaluation must be immutable/auditable.

## Security

- Broker/API credentials must never be committed or exposed to the frontend.
- Secrets must use environment variables or secret management.
- Administrative/configuration operations must be protected.
- Paper execution must remain isolated from future live execution.

## Observability

Log structured events for ingestion, normalization, analysis, prediction, recommendation, risk decisions, paper orders, fills, alerts and job failures.

Metrics should include provider health, job duration/failure, data freshness, recommendation counts, risk blocks, paper-trade outcomes and alert counts.

## Performance — PROPOSED

Initial performance targets should be defined after data-volume and universe decisions. Avoid premature hard limits during discovery.

## Scalability

The design must permit multiple data providers, AI providers and execution providers without rewriting domain logic.

Scale individual workloads independently when actual usage justifies it.

## Maintainability

Prefer small cohesive components, dependency injection, explicit contracts and architecture tests over implicit coupling.

## Auditability

A recommendation and autonomous paper trade must be reconstructable from the input data, analysis outputs, model/version, risk decision and execution result.
