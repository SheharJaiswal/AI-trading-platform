# V3 Dashboard API Map

Dashboard: `/health`, `/api/portfolio`, `/api/alerts`.

Research: `/api/market/{symbol}/quote`, `/api/recommendations/{symbol}`, `/api/ai/research`.

Execution: `/api/paper-trades/{symbol}` with `Idempotency-Key` and server-side risk enforcement.
