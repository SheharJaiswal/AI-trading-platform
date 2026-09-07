# AI Trading Platform — Copilot / Development Instructions

## Project mission

Build an AI-powered Indian equity stock recommendation and paper-trading platform.

The platform combines fundamental analysis, candlestick/technical analysis, latest company/market news, news sentiment, ML/AI-based short-term return prediction, explainable BUY/HOLD/SELL recommendations, autonomous paper trading, continuous background risk monitoring, alerts, portfolio/P&L tracking, and historical evaluation.

Initial scope is **paper trading only**. Do not implement real-money order execution unless explicitly requested and separately approved.

## Product principle

The system should answer:

> Which stock is attractive now, why, what is the expected short-term return, what are the risks, and how would the recommendation have performed?

Recommendations must be evidence-based and explainable. Never present an AI prediction as certainty. Prefer predicting expected return/probability over a defined horizon rather than claiming an exact future price.

## Initial scope

- Market: Indian equities
- Initial universe: configurable, starting with a manageable set such as NIFTY 50
- Prediction horizon: configurable, initially short-term such as 1–5 trading days
- Execution: paper trading only
- Initial market-data/broker provider: Angel One
- Architecture: provider-agnostic so multiple sources can be added later

## Architecture principles

Separate:

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

Conceptual flow:

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

Continuous monitoring flow:

```text
Market + News Events
      ↓
Background Monitoring
      ↓
Risk Evaluation
      ↓
Alerts / Paper Exit Decision
```

## Approved technology stack — mandatory default

Unless a documented architectural decision changes it, use this stack:

- **Frontend:** Angular + TypeScript
- **Backend/API:** C# + ASP.NET Core on .NET 10
- **Core domain/business logic:** C# with strongly typed contracts and clean architecture boundaries
- **Quantitative ML/AI:** Python where justified, exposed through a clear service boundary such as FastAPI
- **Cloud AI:** configurable cloud AI provider, defaulting to OpenAI-compatible/cloud AI capability
- **Local LLM:** Ollama initially, behind the same AI provider abstraction
- **Primary database:** PostgreSQL
- **Time-series market data:** PostgreSQL with TimescaleDB where justified; do not introduce a separate time-series database without an architectural reason
- **Cache/coordination:** Redis where justified
- **Messaging/events:** RabbitMQ initially; consider Kafka only when scale/streaming requirements justify it
- **Background processing:** .NET Worker Services / hosted background services for application-owned jobs
- **Containers:** Docker and Docker Compose for local development and reproducible environments
- **CI/CD:** GitHub Actions
- **Observability:** OpenTelemetry-compatible tracing/metrics and structured logging
- **Testing:** xUnit for .NET and pytest for Python
- **API contract/documentation:** OpenAPI/Swagger

Do not introduce additional frameworks, databases, brokers, or infrastructure technologies without a clear architectural reason documented in the repository.

Do not rewrite the backend in Python merely because the platform uses ML. Keep product/business logic in .NET and use Python for quantitative/ML workloads where its ecosystem provides a meaningful advantage.

## Technology boundary rule

The platform is a **trading platform with an AI/ML intelligence layer**, not an unconstrained AI application.

C#/.NET should own product-critical business logic including:

- API/application orchestration
- Trading domain
- Portfolio and positions
- Paper execution
- Risk engine
- Alerts
- Provider abstractions
- Background workers
- Configuration
- Persistence
- Auditability

Python should own workloads where the Python ecosystem provides a clear advantage, including:

- Feature engineering
- Statistical modelling
- ML training/inference
- NLP/sentiment processing
- Quantitative research
- Backtesting research where appropriate
- Model evaluation

The boundary between .NET and Python must use explicit, versioned, strongly defined contracts. Do not couple core domain models directly to Python implementation details.

## AI provider abstraction — mandatory

AI must be provider-agnostic from the application/business-logic perspective.

The default AI capability is cloud AI, but the system must support switching to a local LLM without changing core business logic.

Use an abstraction conceptually equivalent to:

```csharp
public interface IAiProvider
{
    Task<AiResearchResult> ResearchAsync(
        AiResearchRequest request,
        CancellationToken cancellationToken);

    Task<AiAnalysisResult> AnalyzeAsync(
        AiAnalysisRequest request,
        CancellationToken cancellationToken);

    Task<AiExplanation> ExplainAsync(
        AiExplanationRequest request,
        CancellationToken cancellationToken);
}
```

Provider implementations may include:

```text
IAiProvider
    ├── Cloud/OpenAI provider
    └── Ollama/local LLM provider
```

AI provider selection must be configuration-driven, for example:

```yaml
ai:
  provider: cloud

  cloud:
    provider: openai
    model: <configured-model>

  local:
    provider: ollama
    model: <configured-model>
    endpoint: http://localhost:11434
```

Switching to local AI should require configuration changes, not changes to recommendation, risk, portfolio, or trading business logic.

AI may be used for:

- Financial/company research
- News interpretation
- Event interpretation
- Sentiment interpretation
- Recommendation explanation
- Research questions
- Strategy discussion
- Natural-language summaries over structured platform data

AI/LLM must **never** directly bypass deterministic risk controls or directly execute trades. All recommendations must pass through the deterministic risk engine and paper-execution boundary.

## Provider abstraction — mandatory

Angel One is only the initial provider. Core business logic must never depend directly on Angel One SDK/API types.

Use interfaces/adapters such as:

```text
MarketDataProvider
FundamentalDataProvider
NewsProvider
ExecutionProvider
```

The initial implementation should contain an Angel One adapter. Future providers should be addable without changing domain/business logic.

Provider selection should be configuration-driven, for example:

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

Never hard-code API credentials. Use environment variables/secrets.

## Data provenance

Important data should retain, where practical:

- Provider/source
- Symbol/instrument identifier
- Timestamp
- Data type
- News publication timestamp
- Retrieval timestamp
- Model/version where applicable

This is required for debugging, reproducibility, auditability, and evaluating bad predictions.

## Analysis

### Fundamentals

Support configurable metrics such as revenue growth, profit growth, EPS, P/E, P/B, ROE, ROCE, debt/equity, free cash flow, dividends, ownership, quarterly results, earnings growth, and valuation.

Never fabricate missing data. Represent unavailable metrics explicitly.

### Technical analysis

Use OHLCV data for configurable indicators including SMA/EMA, RSI, MACD, Bollinger Bands, ATR/volatility, volume, support/resistance, trend, and momentum.

### Candlesticks

Candlestick detection should preferably be deterministic/rule-based, not an LLM guessing from an image.

Patterns may include Doji, Hammer, Inverted Hammer, Engulfing, Morning/Evening Star, Harami, Shooting Star, and other well-defined patterns.

Patterns must include timeframe/context and must never automatically imply a trade.

### News

News analysis must be time-aware. Recent events should carry appropriate weight and stale news must not be treated as fresh.

Classify sentiment/event impact as positive, neutral, negative, and material/high-impact where applicable. Identify affected instruments and publication time where possible.

## AI/ML

Do not use an LLM as an unconstrained trading decision-maker.

Use deterministic/rule-based logic for calculations and safety-critical constraints. Use statistical/ML models for prediction and AI for interpretation/explanation where appropriate.

Prediction output should be structured, for example:

```json
{
  "symbol": "RELIANCE",
  "horizonDays": 5,
  "expectedReturn": 0.039,
  "probabilityPositive": 0.72,
  "confidence": 0.81,
  "modelVersion": "..."
}
```

Never invent confidence, prices, financial metrics, news, or performance.

## Recommendation engine

A recommendation should contain:

- BUY / HOLD / SELL
- Expected return
- Confidence
- Prediction horizon
- Entry/reference price
- Target where applicable
- Stop loss where applicable
- Supporting factors
- Risk factors
- Data timestamp
- Model/version where applicable

Recommendations must be explainable using actual signals/data.

## Paper trading

Paper trading is the only execution mode in the initial phase.

Support, as appropriate:

- Virtual cash
- Orders
- Fills
- Positions
- Average entry price
- Realized/unrealized P&L
- Configurable transaction costs
- Stop loss
- Target
- Position sizing
- Portfolio exposure
- Trade history

The paper execution implementation should behave like an execution provider from the domain's perspective.

Every autonomous trade must have an auditable reason and reference to the signals/model output that led to it.

Never claim profitability without clearly stating evaluation period, assumptions, costs, and limitations.

## Autonomous paper-trading loop

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

## Background monitoring — mandatory

Implement a background worker/service for continuous monitoring. It must not contain provider-specific logic.

Monitor, as applicable:

- Latest market prices
- Open positions
- Stop-loss proximity/breaches
- Target proximity
- Sudden price moves
- Abnormal volume
- Technical trend changes
- Candlestick reversals
- Latest company/market news
- Material events
- Sentiment changes
- Portfolio drawdown
- Position concentration
- Sector concentration
- Changes in AI prediction/confidence

Monitoring frequency must be configurable and respect provider rate limits.

Jobs must be restart-safe and idempotent. Retries must not create duplicate orders or alerts.

## Risk engine

Risk management must be deterministic and independent of the LLM.

Support configurable controls such as:

- Maximum position size
- Maximum portfolio exposure
- Maximum sector exposure
- Stop loss
- Maximum drawdown
- Maximum number of open positions
- Minimum confidence threshold
- Maximum daily loss
- Volatility restrictions

A recommendation can never bypass a hard risk rule.

All risk decisions should be auditable.

## Alerts

Support severity levels:

- INFO
- WARNING
- HIGH
- CRITICAL

Prevent alert spam through deduplication and rate limiting. Link alerts to the relevant event/position.

Examples:

```text
HIGH — Position risk increased
CRITICAL — Stop loss breached
WARNING — Negative material news detected
WARNING — AI confidence dropped significantly
INFO — Target nearly reached
```

Alert delivery should be abstracted so UI, email, Telegram, push, and other channels can be added later.

## Evaluation

Record predictions and compare them with actual outcomes.

Track metrics such as directional accuracy, average return, win rate, loss rate, profit factor, maximum drawdown, P&L, performance by strategy/signal/model, and performance by market regime.

Avoid look-ahead bias and data leakage. Never evaluate a model on data used to train/tune it.

Do not hide losses or selectively report profitable trades.

## Engineering standards

- Clean, modular architecture
- Domain logic independent of infrastructure
- Small, testable components
- Dependency injection
- Strongly typed contracts
- External-data validation
- Graceful provider failure handling
- Safe retries with backoff
- Provider rate-limit compliance
- Idempotent background jobs
- UTC timestamps internally
- No secrets in source control
- Structured logging without secrets
- No silent exception swallowing
- No fabricated financial data
- Explicit configuration instead of magic constants

## Repository organization

Prefer architecture-driven boundaries similar to:

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

Do not create empty folders just to match this example. Let the actual architecture determine the final structure.

## Development workflow

Use feature branches for meaningful changes:

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
fixes
  ↓
merge
```

Do not make unrelated changes in a feature branch. Do not merge blindly; review diffs and tests first.

## Documentation

Maintain documentation for architecture and important decisions. Use ADRs where appropriate.

Document provider contracts, data models, prediction methodology, paper-trading assumptions, risk rules, background jobs, alert semantics, evaluation methodology, and known limitations.

## Testing

Business-critical automated tests must cover, as applicable:

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

## Security

- Never commit API keys, tokens, passwords, or broker secrets.
- Use environment variables/secret stores.
- Never expose broker credentials to the frontend.
- Validate external input.
- Protect configuration/admin endpoints.
- Keep paper trading isolated from any future live execution path.

## Working with the user

Before a major implementation:

1. Inspect the existing repository and documentation.
2. Check whether the requirement already exists.
3. Extend existing abstractions instead of duplicating them.
4. Ask when a business rule is genuinely ambiguous.
5. Choose sensible defaults for minor implementation details and document them.
6. Keep the user informed about meaningful architectural trade-offs.

If requirements are still being discovered, remain in Discovery/Design rather than prematurely implementing a large system.

## Definition of Done

A feature is complete only when applicable implementation, tests, configuration/documentation, error handling, logging/observability, security considerations, provider abstraction, and CI are addressed.

No secrets may be committed.

## First implementation milestone

Build a thin vertical slice:

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

Only after this works reliably should we expand ML, fundamentals, news intelligence, and autonomous strategy complexity.
