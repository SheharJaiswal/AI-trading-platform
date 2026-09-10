# Portfolio Risk Policy Safety Gate

Portfolio monitoring thresholds are configuration, not embedded business policy.

- No default position concentration limit is shipped.
- No default gross exposure limit is shipped.
- No default drawdown limit is shipped.
- An unset threshold disables that observation rather than inventing a limit.
- Invalid configured thresholds are rejected.
- Monitoring remains observation-only and cannot execute, cancel, or modify trades.
- The correction is based on current `main` and preserves newer evaluation workspace work.
