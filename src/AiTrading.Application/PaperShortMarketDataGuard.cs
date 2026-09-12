namespace AiTrading.Application;

/// <summary>Deterministic freshness guard for paper-short decisions. It never creates or closes orders.</summary>
public sealed record PaperShortMarketDataGuardResult(bool Approved, string? Reason);

public static class PaperShortMarketDataGuard
{
    public static PaperShortMarketDataGuardResult Validate(
        DateTimeOffset marketDataTimestamp,
        DateTimeOffset evaluationTimestamp,
        TimeSpan maxAge)
    {
        if (maxAge < TimeSpan.Zero)
            return new(false, "INVALID_MAX_AGE");

        if (marketDataTimestamp > evaluationTimestamp)
            return new(false, "FUTURE_MARKET_DATA");

        if (evaluationTimestamp - marketDataTimestamp > maxAge)
            return new(false, "STALE_MARKET_DATA");

        return new(true, null);
    }
}
