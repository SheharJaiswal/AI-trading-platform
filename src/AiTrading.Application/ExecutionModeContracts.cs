namespace AiTrading.Application;

public enum ExecutionMode
{
    Paper,
    Live,
    Backtest,
    Research
}

public sealed record ExecutionContext(ExecutionMode Mode, bool ExplicitlyEnabled = false);

public static class ExecutionModePolicy
{
    public static void RequirePaper(ExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Mode != ExecutionMode.Paper)
            throw new InvalidOperationException($"Paper execution requires {ExecutionMode.Paper} mode; received {context.Mode}.");
    }

    public static void RequireNonLiveSimulation(ExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Mode == ExecutionMode.Live)
            throw new InvalidOperationException("Backtest/research workflows cannot use Live execution mode.");
    }

    public static void RequireExplicitLive(ExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Mode != ExecutionMode.Live)
            throw new InvalidOperationException($"Live execution requires {ExecutionMode.Live} mode; received {context.Mode}.");

        if (!context.ExplicitlyEnabled)
            throw new InvalidOperationException("Live execution requires explicit operator enablement.");
    }

    public static bool CanReachProvider(ExecutionContext context, string provider)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (string.IsNullOrWhiteSpace(provider))
            throw new ArgumentException("Provider is required.", nameof(provider));

        return context.Mode switch
        {
            ExecutionMode.Paper => string.Equals(provider, "paper", StringComparison.OrdinalIgnoreCase),
            ExecutionMode.Live => false,
            ExecutionMode.Backtest or ExecutionMode.Research => false,
            _ => false
        };
    }
}
