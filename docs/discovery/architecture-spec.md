# Architecture Specification

## Approved baseline

```text
Angular + TypeScript
        ↓
ASP.NET Core / .NET 10
        ↓
Application + Domain + Infrastructure
        ↓
PostgreSQL / TimescaleDB
        ↕
Redis
        ↕
RabbitMQ
        ↓
.NET Workers ───────────────┐
                            ↓
                     Python / FastAPI
                            ↓
                    ML / Quant Research

AI Gateway
   ├── Cloud AI (default)
   └── Ollama local LLM
```

## Boundary rules

### .NET

Owns API, application orchestration, domain entities, portfolio, paper execution, risk, alerts, configuration, persistence and background workers.

### Python

Owns ML/statistical workloads, feature engineering, NLP/sentiment where appropriate, quantitative research and model evaluation.

### AI gateway

Owns AI provider selection and normalized AI contracts. Business logic depends on the abstraction, not a vendor SDK.

### Providers

Market data, fundamentals, news and execution are adapter-based. Angel One is an initial implementation, not a domain dependency.

## Initial deployment shape

Prefer a small number of deployable components:

1. Angular frontend
2. ASP.NET Core API
3. .NET Worker
4. Python ML/AI service where required
5. PostgreSQL/TimescaleDB
6. Redis
7. RabbitMQ

Do not split every domain into a separate microservice in V1.

## Data flow

```text
Provider → Adapter → Normalized Contract → Storage
                                      ↓
                           Analysis / Prediction
                                      ↓
                              Recommendation
                                      ↓
                                Risk Engine
                                      ↓
                              Paper Execution
                                      ↓
                           Portfolio / Evaluation
```

## Architectural constraints

- Risk cannot depend on LLM approval.
- AI cannot directly execute trades.
- Provider SDKs cannot leak into domain models.
- Financial calculations must be deterministic.
- Python models must expose versioned contracts.
- Cloud/local AI must be interchangeable through configuration.
