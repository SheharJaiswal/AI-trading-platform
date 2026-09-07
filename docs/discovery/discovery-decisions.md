# Discovery Decisions

## Confirmed

| Area | Decision |
|---|---|
| Market | Indian equities |
| Execution | Paper trading only in V1 |
| Initial market provider | Angel One adapter |
| Provider architecture | Adapter/interface based |
| Frontend | Angular + TypeScript |
| Backend | ASP.NET Core / .NET 10 + C# |
| ML/quant | Python where justified |
| Python API boundary | FastAPI or equivalent explicit contract |
| Database | PostgreSQL |
| Time series | TimescaleDB where justified |
| Cache | Redis where justified |
| Messaging | RabbitMQ initially |
| Background processing | .NET Worker/hosted services |
| Containers | Docker / Docker Compose |
| CI/CD | GitHub Actions |
| AI default | Cloud AI |
| Local AI | Ollama through same AI abstraction |
| AI safety | LLM cannot bypass risk or execute trades |
| Risk | Deterministic and auditable |

## Proposed defaults

These are recommendations, not final product commitments:

- Start with a manageable stock universe such as NIFTY 50.
- Start with a 1–5 trading-day prediction horizon.
- Start with a monolithic/modular .NET application plus separate Python ML service rather than many microservices.
- Use PostgreSQL/TimescaleDB rather than a second specialized database unless scale proves the need.
- Use RabbitMQ before considering Kafka.

## Still open

Numerical trading/risk parameters and several operational decisions remain open. See `open-questions.md`.
