# Risk Management Specification

## Principle

Risk controls are deterministic, auditable and independent of the LLM.

## Candidate controls

- Maximum position size
- Maximum portfolio exposure
- Maximum sector exposure
- Maximum open positions
- Minimum confidence
- Stop loss
- Maximum drawdown
- Maximum daily loss
- Volatility restrictions
- Data freshness requirements

## Decision flow

```text
Recommendation
      ↓
Data validation
      ↓
Risk evaluation
      ↓
Allowed / Blocked
      ↓
Paper execution only if Allowed
```

## Hard rule

No AI response, strategy score or operator request can bypass a configured hard risk rule.

## Audit

Each risk decision should retain the applicable rules, input values, decision, reason and timestamp.

## Open parameters

The exact numerical thresholds are intentionally not finalized during this discovery stage.
