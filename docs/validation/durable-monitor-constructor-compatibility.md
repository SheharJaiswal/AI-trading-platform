# Durable monitor constructor compatibility

The durable monitor preserves the pre-existing `IMonitoringFailureSink` positional constructor argument and appends `PortfolioRiskMonitor` after it. This keeps existing monitoring resilience tests and callers source-compatible while allowing portfolio-risk observation to be injected.

This change does not alter risk thresholds, execution behavior, monitoring cadence, or liquidation behavior.
