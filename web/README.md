# AI Trading Dashboard

Angular V3.1 trading workspace connected to the paper-trading API.

## Run

```bash
npm install
npm start
```

The dashboard uses relative `/health` and `/api/*` endpoints so it can sit behind the same reverse proxy as the API. It contains no broker credentials. Execution remains explicitly paper-only and AI output cannot authorize a trade.

## Current V3.1 slice

- Responsive application shell and navigation.
- Typed health, portfolio, quote and recommendation API models.
- API service using Angular `HttpClient`.
- Dashboard health and portfolio loading states.
- Explicit unavailable/error and retry state.
- Last successful refresh timestamp.
