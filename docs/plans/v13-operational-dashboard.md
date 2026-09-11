# V13 Operational Dashboard Plan

## Objective
Close the remaining FR-14 dashboard gap by giving operators a dedicated read-only operational view across portfolio state, risk observations, alerts, prediction/evaluation status, and paper-session health.

## Scope
- Keep existing paper-trading, monitoring, prediction, evaluation, and backtest services as the source of truth.
- Add a dedicated operational dashboard route rather than overloading the existing portfolio-at-a-glance screen.
- Surface freshness and unavailable-data states explicitly.
- Show risk observations and alerts without adding automatic trading actions.
- Show paper-session state and recent cycle outcomes where existing APIs already expose them.
- Keep AI advisory-only and server-side deterministic risk authoritative.

## Safety gate
- No live broker execution.
- No automatic liquidation.
- No new risk thresholds or market-hours policy.
- No fabricated data when an API is unavailable.
- UI actions must not bypass the existing risk/execution boundaries.

## Acceptance
- Dedicated operational route exists.
- Portfolio, alerts, sessions, and evaluation/backtest status are represented using existing APIs.
- Loading, stale/unavailable, and empty states are explicit.
- Existing dashboard and paper-only workflows remain backward compatible.
- Angular unit tests cover the new operational states.
- .NET and Angular CI pass on the exact feature head before merge.
