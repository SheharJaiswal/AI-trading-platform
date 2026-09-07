# Product Specification

## 1. Product vision

Build an AI-powered Indian equity recommendation and paper-trading platform that combines fundamental analysis, technical/candlestick analysis, current news and sentiment, and quantitative/ML prediction to produce explainable short-term stock recommendations.

The platform must also autonomously paper-trade approved recommendations and continuously monitor positions for risk.

## 2. Core user question

For a selected stock or configured universe, the platform should answer:

> Is this stock attractive now, why, what short-term return is expected, what could invalidate the thesis, what is the risk, and how would the recommendation have performed?

## 3. Primary users

### Retail/investment researcher

Needs to explore stocks, understand evidence, compare opportunities and inspect AI explanations.

### Strategy/research user

Needs to test prediction approaches, compare models and evaluate historical performance.

### Paper-trading operator

Needs to monitor virtual portfolio, positions, orders, P&L, risk and alerts.

## 4. Product principles

1. Paper trading only in the initial phase.
2. Evidence before recommendation.
3. Expected return/probability over a defined horizon rather than false precision.
4. Deterministic risk controls override AI recommendations.
5. Every autonomous trade must be auditable.
6. Current data must have timestamps and provenance.
7. Losses and failed predictions must remain visible in evaluation.
8. Provider implementations must be replaceable.
9. Cloud AI is the default, but local LLM use must be configurable.
10. The platform must be designed for gradual expansion rather than premature microservices complexity.

## 5. V1 scope

### In scope

- Indian equities
- Configurable stock universe, initially a manageable universe such as NIFTY 50
- Angel One as initial market-data/broker adapter
- Normalized market data
- Fundamental analysis
- Technical indicators
- Deterministic candlestick pattern detection
- Current news and sentiment
- Short-term return prediction
- BUY/HOLD/SELL recommendations
- Explainable recommendations
- Deterministic risk engine
- Paper orders and fills
- Portfolio and P&L
- Autonomous paper-trading loop
- Background risk monitoring
- Alerts
- Prediction and strategy evaluation
- Angular dashboard/API visibility

### Out of scope for initial phase

- Real-money execution
- Uncontrolled LLM trading
- High-frequency trading infrastructure
- Guaranteed profit claims
- Fully autonomous live brokerage execution
- Large microservice decomposition without evidence of need

## 6. Success outcomes

V1 is successful when a user can:

1. Select a stock or scan the configured universe.
2. View normalized market data and relevant analysis.
3. See fundamental, technical, candlestick and news evidence.
4. Receive a structured short-term prediction and recommendation.
5. Understand the recommendation and its risks.
6. Apply deterministic risk rules.
7. Create or automatically execute a paper trade.
8. See position and P&L changes.
9. Receive risk alerts from background monitoring.
10. Later compare predictions with actual outcomes.

## 7. Quality bar

The product must prefer an explicit `NO_DECISION`, `INSUFFICIENT_DATA`, or `RISK_BLOCKED` outcome over fabricating information or forcing a trade.
