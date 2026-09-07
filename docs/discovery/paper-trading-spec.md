# Paper Trading Specification

## Objective

Simulate trading realistically enough to evaluate recommendations while keeping execution completely separate from live brokerage execution.

## Required capabilities

- Virtual cash
- Orders
- Fills
- Positions
- Average entry price
- Realized/unrealized P&L
- Transaction costs
- Position sizing
- Stop loss
- Target
- Trade history
- Audit trail

## Order lifecycle

```text
Recommendation
    ↓
Risk validation
    ↓
Paper order
    ↓
Simulated fill
    ↓
Position update
    ↓
P&L update
```

## Auditability

Every autonomous paper order must reference the recommendation, prediction/model output, relevant signals, risk decision and execution result.

## Accounting rules

P&L and portfolio calculations must be deterministic. The system must clearly distinguish realized and unrealized P&L and account for configured transaction costs.

## Proposed defaults — require confirmation

- Starting capital: OPEN
- Position sizing method: OPEN
- Maximum concurrent positions: OPEN
- Stop-loss policy: OPEN
- Target policy: OPEN
- Transaction-cost model: OPEN
- Slippage model: OPEN
- Intraday vs overnight behavior: OPEN
- Market hours/calendar source: OPEN

## Safety

There must be no accidental path from paper execution to real-money execution in V1.
