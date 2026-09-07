# Evaluation Specification

## Objective

Determine whether predictions and strategies provide useful, repeatable results without look-ahead bias or selective reporting.

## Prediction evaluation

Track:

- Directional accuracy
- Expected vs realized return
- Probability calibration where applicable
- Performance by horizon
- Performance by symbol
- Performance by model/version
- Performance by market regime

## Trading evaluation

Track:

- P&L
- Win rate
- Loss rate
- Average win/loss
- Profit factor
- Maximum drawdown
- Exposure
- Transaction costs
- Performance over time

## Integrity rules

- No look-ahead bias.
- No data leakage between training/tuning/evaluation.
- Preserve historical inputs used by a prediction where practical.
- Include costs and stated simulation assumptions.
- Never hide losing predictions or trades.

## Model versioning

Every prediction must identify the model/version and relevant configuration so results can be reproduced and compared.

## Open decisions

- Baseline model
- Training/evaluation split
- Minimum evaluation sample size
- Benchmark definition
- Rebalancing/evaluation cadence
