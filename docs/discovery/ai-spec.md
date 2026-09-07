# AI / ML Specification

## 1. Principle

AI is an intelligence layer, not the authority over trading safety.

## 2. Responsibilities

### Deterministic components

- OHLCV calculations
- Technical indicators
- Candlestick detection
- Fundamental calculations
- Position sizing
- Stop-loss/target calculations where rule-based
- P&L
- Transaction costs
- Risk rules

### ML/statistical components

- Short-term return prediction
- Probability estimation
- Feature engineering
- Model training/evaluation
- Regime/volatility modelling where justified

### LLM/AI components

- Company/market research
- News interpretation
- Event interpretation
- Natural-language synthesis
- Recommendation explanation
- Research interaction

## 3. Provider abstraction

Application code shall depend on an abstraction such as `IAiProvider`.

Initial providers:

- Cloud AI / OpenAI-compatible provider — default
- Ollama — local LLM option

Configuration must select the active provider without changing domain code.

## 4. Grounding rule

AI explanations must be generated from supplied, timestamped evidence. The system must not permit an AI response to invent missing market/fundamental/news facts.

## 5. Prediction contract

Minimum prediction fields:

```json
{
  "symbol": "RELIANCE",
  "horizonDays": 5,
  "expectedReturn": 0.039,
  "probabilityPositive": 0.72,
  "confidence": 0.81,
  "modelVersion": "model-version"
}
```

Prediction contracts must be versioned.

## 6. Safety

The LLM cannot:

- bypass risk rules
- alter portfolio accounting
- directly submit an execution request
- fabricate evidence
- override provider validation

## 7. Research mode vs decision mode

Research responses may be exploratory and conversational. Trading decisions must use structured, validated outputs and deterministic gates.

## 8. Local LLM requirement

Local inference must use the same application-level contract as cloud AI. Ollama endpoint/model details belong to configuration/infrastructure, not business logic.

## 9. Evaluation

AI/ML outputs must be versioned so historical recommendations can be attributed to the exact model/configuration used.
