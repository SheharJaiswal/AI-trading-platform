# Functional Requirements

Status labels: `CONFIRMED`, `PROPOSED`, `OPEN`.

## FR-01 Market data — CONFIRMED

The platform shall ingest market data through a provider abstraction and normalize it into internal contracts.

Acceptance:
- Provider-specific SDK types do not cross into domain logic.
- Symbol/instrument identity is retained.
- Timestamps are retained.
- Provider failures are observable and do not corrupt stored data.

## FR-02 Stock analysis — CONFIRMED

The platform shall calculate technical indicators and deterministic candlestick patterns from OHLCV data.

Acceptance:
- Indicators are reproducible from stored inputs.
- Patterns include timeframe and context.
- Missing/insufficient candles are handled explicitly.
- An individual pattern never automatically implies a trade.

## FR-03 Fundamental analysis — CONFIRMED

The platform shall expose configurable fundamental metrics with source and timestamp metadata.

Acceptance:
- Missing metrics remain unavailable rather than fabricated.
- Metrics can be traced to their source.
- Stale data can be identified.

## FR-04 News intelligence — CONFIRMED

The platform shall ingest relevant company/market news through a provider abstraction and classify recency, sentiment and materiality.

Acceptance:
- Publication time is retained where available.
- Stale news is distinguishable from current news.
- Affected instruments are identified where possible.

## FR-05 Prediction — CONFIRMED

The platform shall produce a structured short-term prediction for configured instruments and horizons.

Minimum output:
- symbol
- horizon
- expected return
- probability of positive return
- confidence
- model/version
- data timestamp

## FR-06 AI research and explanation — CONFIRMED

The platform shall use an `IAiProvider` abstraction for cloud and local AI providers.

Acceptance:
- Cloud AI is the default configuration.
- Local LLM can be selected by configuration.
- Changing AI provider does not require changing risk/trading domain logic.
- AI receives grounded structured evidence.
- AI cannot directly execute or bypass trades/risk rules.

## FR-07 Recommendation — CONFIRMED

The platform shall generate BUY/HOLD/SELL recommendations from evidence and prediction outputs.

A recommendation shall contain expected return, confidence, horizon, reference price, risk factors, supporting factors, timestamp and model/version where applicable.

## FR-08 Risk decision — CONFIRMED

Every trade candidate shall pass deterministic risk validation before paper execution.

A failed hard rule shall produce an auditable rejection/block reason.

## FR-09 Paper execution — CONFIRMED

The platform shall support virtual cash, orders, fills, positions, average price, realized/unrealized P&L, costs, stop loss and target handling.

## FR-10 Autonomous loop — CONFIRMED

The platform shall support a configurable autonomous loop:

Scan → Analyze → Predict → Rank → Recommend → Risk check → Paper order → Fill → Monitor.

## FR-11 Background monitoring — CONFIRMED

The platform shall continuously monitor open positions, market conditions, news and portfolio risk using restart-safe/idempotent jobs.

## FR-12 Alerts — CONFIRMED

The platform shall generate severity-based alerts and deduplicate repeated alerts.

## FR-13 Evaluation — CONFIRMED

The platform shall record predictions and outcomes so directional accuracy, returns, win/loss statistics, drawdown and P&L can be evaluated without look-ahead bias.

## FR-14 Dashboard/API — PROPOSED

The initial UI should provide:
- Market/stock search
- Stock analysis view
- Recommendation view
- Open positions
- Orders/trades
- Portfolio/P&L
- Risk state
- Alerts
- Prediction/evaluation history

## FR-15 No-decision states — CONFIRMED

The platform shall support explicit non-trading outcomes including insufficient data, stale data, risk blocked, low confidence and no actionable opportunity.
