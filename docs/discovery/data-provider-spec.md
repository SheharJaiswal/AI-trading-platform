# Data Provider Specification

## Provider interfaces

The platform should define application/domain-facing contracts equivalent to:

```text
IMarketDataProvider
IFundamentalDataProvider
INewsProvider
IExecutionProvider
```

## Initial provider

Angel One is the initial market-data/broker integration.

Execution remains paper-only in V1 through a dedicated paper execution provider.

## Normalization

External provider payloads must be mapped into internal normalized contracts before entering domain workflows.

Normalized records should retain:

- Internal symbol
- Provider symbol/instrument ID
- Provider name
- Event/data timestamp
- Retrieval timestamp
- Data type
- Source metadata

## Data freshness

Each data class must have an explicit freshness policy. Stale data must be detectable and must not silently enter a current recommendation.

## Provider failure

Provider errors must be classified, logged and surfaced through health/observability mechanisms. Retry policies must use bounded retries and backoff where appropriate.

## Credentials

Credentials are configuration/secrets only. They must never be stored in source control or sent to the browser.

## Future providers

The architecture should allow another market-data, news, fundamentals or execution provider to be added without modifying core domain logic.
