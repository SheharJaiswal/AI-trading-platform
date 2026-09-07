using AiTrading.Application;
using AiTrading.Domain;

namespace AiTrading.Infrastructure;

/// <summary>Deterministic provider for local development and automated tests.</summary>
public sealed class DemoMarketDataProvider : IMarketDataProvider
{
    public Task<MarketQuote> GetQuoteAsync(Symbol symbol, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        return Task.FromResult(new MarketQuote(symbol, "NSE", symbol.InstrumentToken ?? "DEMO", now, 100m, 104m, 99m, 103m, 103m, 100000, "demo"));
    }

    public Task<IReadOnlyList<Candle>> GetCandlesAsync(Symbol symbol, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
    {
        var end = to == default ? DateTimeOffset.UtcNow : to;
        var candles = Enumerable.Range(0, 30).Select(i =>
        {
            var close = 95m + i * 0.28m;
            return new Candle(end.AddDays(-29 + i), close - 0.8m, close + 1.4m, close - 1.2m, close, 100000 + i * 1000);
        }).ToArray();
        return Task.FromResult<IReadOnlyList<Candle>>(candles);
    }
}
