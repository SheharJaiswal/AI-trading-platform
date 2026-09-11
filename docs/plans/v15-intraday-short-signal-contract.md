# V15 Intraday Short Signal Contract

## Goal

Expose a deterministic, paper-only bearish candidate from the existing recommendation pipeline without turning a bearish recommendation into a broker order.

## Contract

- `RecommendationAction.Sell` represents a bearish candidate; it is a signal, not an execution instruction.
- The autonomous paper cycle may rank and return bearish candidates for operator visibility.
- A short position may only be created through the dedicated paper-short lifecycle and the existing deterministic risk boundary.
- Stop-loss for a short must be strictly above entry; target must be strictly below entry.
- Short unrealized P&L is `(entryPrice - currentPrice) * quantity`.
- A price at or above the stop is a stop event; a price at or below the target is a target event.
- Invalid short levels produce `NO_TRADE`.
- Missing or stale market data must not create an executable short.
- No live broker adapter, automatic liquidation, or AI risk bypass is introduced by this slice.

## Intraday safety

The contract is intentionally interval-agnostic. Market-hours, candle timeframe, slippage policy, notification policy, and exact numerical entry/stop/target formulas remain configuration/business decisions and are not invented here.

## Agent convergence checkpoint

- **BA:** closes the remaining documented gap between bearish recommendations and operator-visible paper workflow without inventing business policy.
- **Senior Engineer:** preserves provider abstraction, deterministic risk authority, and separation between recommendation and execution.
- **Trader/Safety:** bearish evidence remains advisory; any short execution remains paper-only and risk-gated.
- **Developer:** implementation must preserve existing long behavior and exact-head CI remains the acceptance gate.
