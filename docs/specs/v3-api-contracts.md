# V3 Dashboard API Contract Requirements

The dashboard consumes existing server-owned contracts and must not reproduce domain logic.

## Required resources

- `GET /health`
- `GET /api/portfolio`
- `GET /api/alerts`
- `GET /api/market/{symbol}/quote?instrumentToken=...`
- `GET /api/recommendations/{symbol}?instrumentToken=...`
- `POST /api/ai/research`
- `POST /api/paper-trades/{symbol}?instrumentToken=...&quantity=...`

## Client rules

- Treat API responses as untrusted input and validate expected shapes.
- Display paper mode explicitly.
- Preserve server risk decision and error codes.
- Never calculate or override execution authorization in the client.
- Send `Idempotency-Key` for paper-trade requests.
