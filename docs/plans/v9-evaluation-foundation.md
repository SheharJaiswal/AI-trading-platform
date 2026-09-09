# V9 Prediction Evaluation Foundation

## BA objective

Close the first implementation slice of FR-13 by recording structured predictions and later outcomes, then calculating deterministic evaluation metrics. This slice does not train models or authorize trades.

## Engineering scope

- Persist prediction metadata: symbol, horizon, expected return, positive-return probability, confidence, model version, and data timestamp.
- Persist exactly one outcome per prediction.
- Reject an outcome timestamp earlier than the prediction data timestamp to prevent obvious look-ahead contamination.
- Expose evaluation history and aggregate metrics through the API.
- Calculate directional accuracy, wins/losses, cumulative return, unit P&L, and peak-to-trough drawdown from outcome order.
- Keep evaluation separate from paper execution and risk authorization.

## Trader-safety constraints

- Evaluation is observational and cannot create or modify orders, fills, positions, or risk decisions.
- No future market data is accepted as prediction input through the evaluation boundary; outcomes are recorded only after the prediction timestamp.
- Model/version and source data timestamp remain attached to each prediction for auditability.

## Acceptance

1. Predictions and outcomes survive application restart when MySQL persistence is enabled.
2. Duplicate outcomes are rejected.
3. Outcome timestamps earlier than prediction data timestamps are rejected.
4. Metrics are deterministic from persisted outcomes and ordered by outcome timestamp.
5. .NET and Docker CI remain green.
