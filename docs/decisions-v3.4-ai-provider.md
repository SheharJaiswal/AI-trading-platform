# V3.4 AI provider boundary decision

## Decision

The application selects `disabled`, `local`, or `cloud` through configuration. Local and cloud modes use the same provider-neutral HTTP adapter and the existing `IAiProvider` contract.

## Configuration

- `AI:Provider` selects the mode.
- `AI:local:Endpoint` and `AI:cloud:Endpoint` define provider endpoints.
- API keys are read from configuration so deployments can inject secrets through environment variables or secret stores; no credentials are committed.
- Provider failures become non-authoritative research results with zero confidence and a machine-readable risk code.

## Safety

The AI adapter only implements `IAiProvider.ResearchAsync`. It has no access to paper-trade execution services, risk decisions, order identifiers, or broker credentials. AI output therefore remains advisory and cannot authorize a trade.
