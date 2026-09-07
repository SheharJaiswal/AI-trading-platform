# V1 Vertical Slice Specification

Status: CONFIRMED FOR IMPLEMENTATION

## Goal

Prove the platform's core safety and domain flow end-to-end with a thin, provider-agnostic paper-trading slice before adding ML, fundamentals, news intelligence, or autonomous strategy complexity.

## Scope

1. Read normalized NSE market data through a provider boundary.
2. Support an Angel One market-data adapter using SmartAPI.
3. Calculate a small deterministic technical signal set from OHLCV.
4. Detect a limited set of deterministic candlestick patterns.
5. Produce a typed recommendation.
6. Apply deterministic risk checks.
7. Execute an approved recommendation only through a paper execution provider.
8. Maintain positions and realized/unrealized P&L in memory for the first slice.
9. Run a background monitor that detects a hard stop-loss breach and emits an idempotent alert.
10. Expose API endpoints for quote/recommendation/portfolio visibility.

## Explicit non-goals

- Real-money execution.
- LLM-driven trading decisions.
- ML training or inference.
- Fundamental data ingestion.
- News ingestion/sentiment.
- Persistent database implementation beyond contracts and configuration scaffolding.
- Complex portfolio optimization.
- Short selling.
- Intraday leverage.

## Domain contracts

### Market data

A normalized quote contains symbol, exchange, instrument token, timestamp, open, high, low, close, volume, and source.

The provider boundary must not expose Angel One DTOs to application/domain code.

### Technical analysis

V1 calculates:
- SMA-20 when enough observations exist.
- RSI-14 when enough observations exist.
- Daily percentage change.

Missing history must result in `INSUFFICIENT_DATA`, not fabricated values.

### Candlestick analysis

V1 supports deterministic detection of:
- Doji
- Hammer
- Shooting Star
- Bullish/Bearish Engulfing

Patterns are evidence only and never independently authorize an order.

### Recommendation

The recommendation contract contains:
- symbol
- action: BUY/HOLD/SELL/NO_DECISION
- reference price
- expected return (nullable in this slice)
- confidence
- horizon days
- supporting signals
- risk factors
- generated timestamp
- model/strategy version

For V1, recommendation logic is deterministic and explicitly marked as `baseline-v1`; no claim of predictive accuracy is made.

### Risk

V1 hard rules:
- Only BUY orders are supported.
- No order may exceed available virtual cash.
- Position quantity must be positive.
- A recommendation with `NO_DECISION` cannot be executed.
- A recommendation blocked by risk cannot be executed.
- A stop-loss breach on an open long position must produce a HIGH alert.

Numerical portfolio limits beyond these basic invariants remain configurable/open in the broader discovery spec and are not invented here.

### Paper execution

The execution provider returns an immutable fill containing order id, symbol, side, quantity, price, timestamp, and source.

Execution is virtual only. No Angel One order endpoint is called.

### Monitoring

The background monitor is an application-owned hosted service. It receives a clock and provider abstractions through dependency injection so it can be tested without real time or network calls.

A stop-loss alert is deduplicated using a stable key based on position id, rule, and evaluation timestamp bucket.

## Acceptance criteria

- [ ] Application code compiles on .NET 10.
- [ ] Domain tests cover technical calculations and candlestick detection.
- [ ] Recommendation tests cover BUY/HOLD/NO_DECISION paths.
- [ ] Risk tests prove blocked recommendations cannot reach execution.
- [ ] Paper execution tests prove cash, position, and P&L invariants.
- [ ] Monitoring tests prove a stop-loss breach emits one alert and retries do not duplicate it.
- [ ] Angel One adapter maps SmartAPI market responses into normalized contracts without leaking provider DTOs.
- [ ] No test requires real Angel One credentials.
- [ ] No code path in the vertical slice can place a real order.
- [ ] API responses use stable JSON contracts and OpenAPI documentation.
- [ ] Structured logs contain correlation information but no credentials.

## Data provenance

All normalized market data and generated recommendations carry source/timestamp metadata. Historical evaluations must be able to distinguish provider data from generated analysis.

## Failure behavior

Provider failures are surfaced as typed application errors. Stale or missing market data prevents a recommendation from becoming executable. Background retries use bounded backoff and must be idempotent.
