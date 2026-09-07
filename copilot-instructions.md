# AI Trading Platform — Copilot / Development Instructions

## 1. Project mission

Build an AI-powered Indian equity stock recommendation and paper-trading platform.

The platform combines:

- Fundamental analysis
- Candlestick and technical analysis
- Latest company/market news
- News sentiment and event impact
- ML/AI-based short-term return prediction
- Explainable BUY / HOLD / SELL recommendations
- Autonomous paper trading
- Continuous background risk monitoring
- User alerts
- Portfolio and P&L tracking
- Historical evaluation of AI recommendations

Initial scope is **paper trading only**. Do not implement real-money order execution unless explicitly requested and separately approved.

## 2. Product principle

The system must answer:

> Which stock is attractive now, why, what is the expected short-term return, what are the risks, and how would the recommendation have performed?

Recommendations must be evidence-based and explainable. Never present an AI prediction as certainty.

Prefer predicting **expected return / probability over a defined horizon** over pretending to predict an exact future price with certainty.

## 3. Initial market scope

V1 targets Indian equities, initially a manageable stock universe such as NIFTY 50.

Keep the universe configurable so it can expand later.

Initial prediction horizon should be configurable, with a short-term focus such as 1–5 trading days.

## 4. High-level architecture

Use clear separation between:

1. Provider/infrastructure layer
2. Data ingestion and normalization
3. Fundamental analysis
4. Technical/candlestick analysis
5. News and sentiment analysis
6. AI/ML prediction
7. Recommendation engine
8. Risk management
9. Paper trading
10. Background monitoring
11. Alerting
12. Portfolio/P&L and evaluation
13. API/UI

A conceptual flow is:

```text
External Providers
      ↓
Data Ingestion
      ↓
Normalization
      ↓
Analysis Engines
      ↓
AI/ML Prediction
      ↓
Recommendation
      ↓
Risk Management
      ↓
Paper Trading
      ↓
Portfolio / P&L
      ↓
Evaluation
```

A separate continuous loop must monitor open positions and new events:

```text
Market + News Events
      ↓
Background Monitoring
      ↓
Risk Evaluation
      ↓
Alerts / Paper Exit Decision
```

## 5. Provider abstraction — mandatory

Angel One is the initial provider, but the core application must NOT depend directly on Angel One APIs.

Use provider interfaces/adapters so providers are replaceable and multiple providers can coexist.

Examples of future providers may include other Indian brokers, market-data vendors, financial-data providers, and news providers.

Core business logic should call abstractions such as:

```text
MarketDataProvider
FundamentalDataProvider
NewsProvider
ExecutionProvider
```

and never call Angel One SDK/API types directly from domain/business logic.

The initial implementation should provide an `AngelOne` adapter.

Provider selection must be configuration-driven, for example:

```yaml
providers:
  marketData:
    active: angelOne
  execution:
    active: paper
  news:
    active: <configured-provider>
  fundamentals:
    active: <configured-provider>
```

Do not hard-code API credentials. Use environment variables/secrets and provide safe configuration examples only.

## 6. Data provenance

Important data must retain provenance where practical:

- Provider/source
- Symbol/instrument identifier
- Timestamp
- Data type
- Publication timestamp for news
- Retrieval timestamp
- Relevant version/model where applicable

This is necessary for debugging, reproducibility, auditability, and evaluating bad predictions.

## 7. Analysis requirements

### Fundamental analysis

Support configurable metrics such as:

- Revenue growth
- Profit growth
- EPS
- P/E
- P/B
- ROE
- ROCE
- Debt/equity
- Free cash flow
- Dividend history
- Institutional/promoter ownership where data is available
- Quarterly results
- Earnings growth
- Valuation

Do not assume all providers expose all metrics. Missing data must be represented explicitly rather than fabricated.

### Technical analysis

Support OHLCV-based analysis including configurable indicators such as:

- SMA/EMA
- RSI
- MACD
- Bollinger Bands
- ATR/volatility
- Volume
- Support/resistance
- Trend/momentum

### Candlestick analysis

Candlestick pattern detection should preferably be deterministic/rule-based rather than relying on an LLM to visually guess chart patterns.

Examples:

- Doji
- Hammer
- Inverted hammer
- Engulfing patterns
- Morning/evening star
- Harami
- Shooting star
- Other well-defined patterns

Patterns must include context and timestamp/timeframe where applicable. A pattern alone must never automatically imply a trade.

### News analysis

News must be time-aware. Recent events should be weighted appropriately, and stale news should not be treated as fresh.

The system should distinguish:

- Positive
- Neutral
- Negative
- Material/high-impact events

Where possible, identify the affected company/instrument and publication time.

## 8. AI/ML design principles

Do not use an LLM as an unconstrained trading decision-maker.

Use deterministic/rule-based components for calculations and safety-critical constraints. Use ML/statistical models for prediction and AI for interpretation/explanation where appropriate.

The prediction layer should expose structured outputs such as:

```text
Expected return
Probability of positive return
Probability of exceeding threshold
Confidence
Prediction horizon
Model/version
```

Example:

```json
{
  "symbol": "RELIANCE",
  "horizonDays": 5,
  "expectedReturn": 0.039,
  "probabilityPositive": 0.72,
  "confidence": 0.81
}
```

Never invent confidence values, financial metrics, news, prices, or model performance.

## 9. Recommendation engine

Recommendations must be structured:

```text
BUY / HOLD / SELL
Expected return
Confidence
Prediction horizon
Entry/reference price
Target where applicable
Stop loss where applicable
Supporting factors
Risk factors
Data timestamp
```

Recommendations should be explainable using the actual signals/data that contributed to them.

Keep recommendation logic separate from UI and provider implementations.

## 10. Paper trading — initial execution model

Paper trading is the only execution mode for the initial phase.

The paper broker/execution implementation should behave like a broker adapter from the domain's perspective.

It must support, as appropriate:

- Virtual cash
- Orders
- Fills
- Positions
- Average entry price
- Realized P&L
- Unrealized P&L
- Fees/transaction costs where configured
- Stop loss
- Target
- Position sizing
- Portfolio exposure
- Trade history

All assumptions must be configurable and documented.

Do not claim profitability from backtests or paper trading without clearly stating the test period, assumptions, costs, and limitations.

## 11. Autonomous paper-trading loop

The AI should be capable of:

```text
Scan universe
  ↓
Analyze candidates
  ↓
Generate predictions
  ↓
Rank opportunities
  ↓
Generate recommendations
  ↓
Apply deterministic risk rules
  ↓
Create paper orders
  ↓
Record fills
  ↓
Monitor positions
```

Every autonomous trade must have an auditable reason and reference to the signals/model output that led to it.

## 12. Background monitoring service — mandatory

The platform must contain a background worker/service for continuous monitoring.

It should periodically or event-driven check:

- Latest market prices
- Open positions
- Stop-loss proximity/breaches
- Target proximity
- Sudden price moves
- Abnormal volume
- Technical trend changes
- Candlestick reversals
- Latest company/market news
- Material negative/positive events
- News sentiment changes
- Portfolio drawdown
- Position concentration
- Sector concentration
- Changes in AI prediction/confidence

The worker must not contain provider-specific logic. It should depend on provider abstractions and domain services.

Monitoring frequency must be configurable and must respect provider rate limits.

The service must be safe to restart and should avoid creating duplicate alerts/orders after retries.

## 13. Risk engine

Risk management must be deterministic and independent from the LLM.

It should support configurable controls such as:

- Maximum position size
- Maximum portfolio exposure
- Maximum sector exposure
- Stop loss
- Maximum drawdown
- Maximum number of open positions
- Minimum confidence threshold
- Maximum daily loss
- Volatility restrictions

Risk decisions should be auditable.

A recommendation must never bypass a hard risk rule.

## 14. Alerts

Support severity levels such as:

- INFO
- WARNING
- HIGH
- CRITICAL

Avoid alert spam. Alerts should be deduplicated, rate-limited, and linked to the underlying event/position where possible.

Examples:

```text
HIGH — Position risk increased
CRITICAL — Stop loss breached
WARNING — Negative material news detected
WARNING — AI confidence dropped significantly
INFO — Target nearly reached
```

Alert delivery should be abstracted so channels can later include UI notifications, email, Telegram, push notifications, etc.

## 15. Evaluation is a first-class feature

The system must record predictions and compare them with actual outcomes.

Track metrics such as:

- Prediction accuracy
- Directional accuracy
- Average return after recommendation
- Win rate
- Loss rate
- Profit factor
- Maximum drawdown
- Sharpe-like metrics where appropriate
- P&L
- Performance by strategy/signal/model
- Performance by market regime

Never optimize a model solely on the same data used to evaluate it.

Avoid look-ahead bias and data leakage in all backtesting/evaluation work.

## 16. Engineering principles

- Prefer clean, modular architecture.
- Keep domain logic independent of infrastructure.
- Favor small, testable services/classes.
- Use dependency injection.
- Use strongly typed models/contracts.
- Validate external data.
- Handle provider failures gracefully.
- Implement retries only where safe and use appropriate backoff.
- Respect API rate limits.
- Make background jobs idempotent.
- Store timestamps consistently; prefer UTC internally.
- Keep secrets out of source control.
- Log useful structured context without logging secrets.
- Do not silently swallow exceptions.
- Do not fabricate missing financial data.
- Prefer explicit configuration over magic constants.

## 17. Technology direction

The technology stack should support the user's existing strengths while allowing a dedicated ML component.

Preferred direction unless discovery changes it:

- Backend/API: C# / ASP.NET Core
- Frontend: Angular initially
- ML/AI: Python service/component where appropriate
- Database: PostgreSQL
- Cache/coordination: Redis where justified
- Containers: Docker
- CI/CD: GitHub Actions

Do not introduce unnecessary technologies merely for novelty.

## 18. Repository organization

Prefer a structure along these lines, adapting as implementation evolves:

```text
backend/
frontend/
trading-engine/
market-data/
strategies/
backtesting/
risk-management/
ai/
database/
infrastructure/
docs/
tests/
```

The final structure should follow actual architectural boundaries rather than forcing empty projects/folders.

## 19. Development workflow

Use feature branches for meaningful changes.

Preferred workflow:

```text
main
  ↓
feature/<short-description>
  ↓
implementation + tests
  ↓
Pull Request
  ↓
review / CI
  ↓
fixes if needed
  ↓
merge
```

Do not make broad unrelated changes in a feature branch.

Do not merge code blindly. Review the diff and tests before merging.

## 20. Documentation requirements

Maintain architecture and decision documentation as the project grows.

Important decisions should be recorded as ADRs where appropriate.

Document:

- Architecture
- Provider contracts
- Data models
- Prediction methodology
- Paper-trading assumptions
- Risk rules
- Background jobs
- Alert semantics
- Evaluation methodology
- Known limitations

## 21. Testing requirements

For business-critical logic, provide automated tests covering at least:

- Candlestick detection
- Technical calculations
- Recommendation logic
- Risk rules
- Position sizing
- Order/fill behavior
- P&L calculations
- Alert generation/deduplication
- Provider mapping/normalization
- Background-job idempotency

Use deterministic fixtures for financial calculations.

## 22. Security

- Never commit API keys, access tokens, passwords, or broker secrets.
- Use environment variables/secret stores.
- Validate and sanitize external input.
- Protect administrative and configuration endpoints.
- Do not expose broker credentials to the frontend.
- Keep paper trading isolated from any future live execution path.

## 23. Financial safety and product language

This is a research/recommendation and paper-trading system during V1.

Do not describe predictions as guaranteed.
Do not hide losses or selectively display profitable trades.
Do not optimize UI or metrics to create a misleading impression of performance.
Always expose relevant assumptions and evaluation periods.

## 24. Working with the user

Before implementing a major feature:

1. Check existing repository structure and documentation.
2. Determine whether the requirement is already covered.
3. Prefer extending existing abstractions over creating duplicates.
4. For ambiguous business rules, ask before making assumptions.
5. For small implementation details, choose sensible defaults and document them.
6. Keep the user informed about architectural trade-offs.

When requirements are not yet finalized, stay in Discovery/Design rather than prematurely implementing a large system.

## 25. Definition of Done

A feature is not complete merely because it compiles.

Where applicable, completion requires:

- Implementation
- Automated tests
- Configuration/documentation
- Error handling
- Logging/observability
- Security review for sensitive areas
- Provider abstraction preserved
- No secrets committed
- CI passing
- Clear PR description

## 26. First milestone

The first implementation milestone should establish a thin vertical slice:

```text
Angel One adapter
      ↓
Normalized market data
      ↓
Basic technical/candlestick analysis
      ↓
Simple recommendation contract
      ↓
Paper order
      ↓
Position/P&L
      ↓
Background risk monitor
      ↓
Alert
      ↓
Dashboard/API visibility
```

Only after this vertical slice works reliably should we expand the ML, fundamentals, news intelligence, and autonomous strategy complexity.
