# V3 Acceptance Criteria

V3 is accepted only when:

1. Dashboard can load durable portfolio, positions and alerts.
2. Research can display quote and recommendation with freshness/provenance.
3. AI research is clearly advisory and cannot trigger execution.
4. Paper-trade confirmation requires explicit user action and `Idempotency-Key`.
5. Server risk decisions remain authoritative; blocked trades cannot execute.
6. Frontend tests and .NET tests pass in CI.
7. No credentials/secrets are exposed in frontend assets.
8. BA, Senior Engineer and Trader/Safety reviews have no blocking findings.
