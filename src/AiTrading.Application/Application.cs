using AiTrading.Domain;

namespace AiTrading.Application;

public interface IMarketDataProvider
{
    Task<MarketQuote> GetQuoteAsync(Symbol symbol, CancellationToken cancellationToken);
    Task<IReadOnlyList<Candle>> GetCandlesAsync(Symbol symbol, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken);
}

public interface IPaperExecutionProvider
{
    Task<Fill> ExecuteAsync(PaperOrder order, CancellationToken cancellationToken);
}

public interface IPaperTradeService
{
    Task<(RiskResult Risk, FillState? Fill)> ExecuteAsync(Guid portfolioId, Guid orderId, string idempotencyKey, Symbol symbol, int quantity, CancellationToken cancellationToken);
}

public interface IPortfolio
{
    Portfolio Snapshot();
    void Apply(Fill fill);
    void SetStopLoss(Guid positionId, decimal stopLoss);
    void UpdateMarketPrice(Symbol symbol, decimal price);
}

public interface IAlertStore
{
    bool TryAdd(Alert alert);
    IReadOnlyList<Alert> GetAll();
}

public sealed class InMemoryAlertStore : IAlertStore
{
    private readonly Dictionary<string, Alert> _alerts = new();
    private readonly object _gate = new();

    public bool TryAdd(Alert alert)
    {
        lock (_gate) return _alerts.TryAdd(alert.Key, alert);
    }

    public IReadOnlyList<Alert> GetAll()
    {
        lock (_gate) return _alerts.Values.OrderByDescending(x => x.CreatedAt).ToArray();
    }
}

public sealed record MarketDataFreshnessOptions(TimeSpan MaxAge)
{
    public static MarketDataFreshnessOptions Default => new(TimeSpan.FromMinutes(5));
}

public sealed class RecommendationService(IMarketDataProvider marketData, MarketDataFreshnessOptions? freshness = null)
{
    private readonly MarketDataFreshnessOptions _freshness = freshness ?? MarketDataFreshnessOptions.Default;

    public async Task<Recommendation> GetRecommendationAsync(Symbol symbol, CancellationToken cancellationToken)
    {
        var quote = await marketData.GetQuoteAsync(symbol, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var age = now - quote.Timestamp;
        if (quote.Timestamp > now || age > _freshness.MaxAge)
        {
            return new(symbol, RecommendationAction.NoDecision, quote.LastTradedPrice, null, 0, 1, [], ["STALE_OR_INVALID_MARKET_DATA"], now, "baseline-v1");
        }

        var candles = await marketData.GetCandlesAsync(symbol, quote.Timestamp.AddDays(-40), quote.Timestamp, cancellationToken);
        if (candles.Count < 20)
            return new(symbol, RecommendationAction.NoDecision, quote.LastTradedPrice, null, 0, 1, [], ["INSUFFICIENT_DATA"], now, "baseline-v1");

        var technical = TechnicalAnalysis.Snapshot(candles);
        var patterns = CandlestickAnalysis.Detect(candles);
        var signals = new List<string>();
        if (technical.Sma20 is { } sma && quote.LastTradedPrice > sma) signals.Add("PRICE_ABOVE_SMA20");
        if (technical.Rsi14 is { } rsi && rsi < 70) signals.Add("RSI_NOT_OVERBOUGHT");
        signals.AddRange(patterns.Where(x => x.Bullish).Select(x => $"BULLISH_{x.Name.Replace(' ', '_').ToUpperInvariant()}"));
        var bullish = signals.Count >= 2;
        return new(symbol, bullish ? RecommendationAction.Buy : RecommendationAction.Hold, quote.LastTradedPrice, null, bullish ? 0.60m : 0.40m, 1, signals, [], now, "baseline-v1");
    }
}

public sealed class RiskEngine
{
    public RiskResult Evaluate(Recommendation recommendation, decimal availableCash, int quantity)
    {
        if (recommendation.RiskFactors.Contains("STALE_OR_INVALID_MARKET_DATA"))
            return new(RiskDecision.InsufficientData, "Market data is stale or invalid.");
        if (recommendation.Action == RecommendationAction.NoDecision) return new(RiskDecision.InsufficientData, "Recommendation does not contain enough data.");
        if (recommendation.Action != RecommendationAction.Buy) return new(RiskDecision.RiskBlocked, "Only BUY paper orders are enabled in V1.");
        if (quantity <= 0) return new(RiskDecision.RiskBlocked, "Order quantity must be positive.");
        if (recommendation.ReferencePrice * quantity > availableCash) return new(RiskDecision.RiskBlocked, "Order exceeds available virtual cash.");
        return new(RiskDecision.Approved, null);
    }
}

public sealed class PaperTradingService(RecommendationService recommendations, RiskEngine risk, IPaperExecutionProvider execution, IPortfolio portfolio)
{
    public async Task<(RiskResult Risk, Fill? Fill)> ExecuteAsync(Symbol symbol, int quantity, CancellationToken cancellationToken)
    {
        var recommendation = await recommendations.GetRecommendationAsync(symbol, cancellationToken);
        var riskResult = risk.Evaluate(recommendation, portfolio.Snapshot().Cash, quantity);
        if (riskResult.Decision != RiskDecision.Approved) return (riskResult, null);
        var order = new PaperOrder(Guid.NewGuid(), symbol, OrderSide.Buy, quantity, recommendation.ReferencePrice, DateTimeOffset.UtcNow);
        var fill = await execution.ExecuteAsync(order, cancellationToken);
        portfolio.Apply(fill);
        portfolio.UpdateMarketPrice(symbol, recommendation.ReferencePrice);
        return (riskResult, fill);
    }
}

public sealed class RiskMonitor(IMarketDataProvider marketData, IPortfolio portfolio, IAlertStore alerts)
{
    public async Task CheckOnceAsync(CancellationToken cancellationToken)
    {
        foreach (var position in portfolio.Snapshot().Positions)
        {
            if (position.StopLoss is null) continue;
            var quote = await marketData.GetQuoteAsync(position.Symbol, cancellationToken);
            portfolio.UpdateMarketPrice(position.Symbol, quote.LastTradedPrice);
            if (quote.LastTradedPrice <= position.StopLoss)
            {
                var bucket = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 60;
                var key = $"{position.Id}:STOP_LOSS:{bucket}";
                alerts.TryAdd(new Alert(key, AlertSeverity.High, $"Stop loss breached for {position.Symbol} at {quote.LastTradedPrice}.", DateTimeOffset.UtcNow, position.Symbol));
            }
        }
    }
}

public sealed class PaperPortfolio(decimal startingCash) : IPortfolio
{
    private decimal _cash = startingCash;
    private decimal _realized;
    private readonly Dictionary<Symbol, Position> _positions = new();
    private readonly Dictionary<Symbol, decimal> _marketPrices = new();

    public Portfolio Snapshot()
    {
        var unrealized = _positions.Values.Sum(position =>
        {
            var marketPrice = _marketPrices.GetValueOrDefault(position.Symbol, position.AverageEntryPrice);
            return (marketPrice - position.AverageEntryPrice) * position.Quantity;
        });
        return new(_cash, _positions.Values.ToArray(), unrealized, _realized);
    }

    public void Apply(Fill fill)
    {
        var value = fill.Price * fill.Quantity;
        if (fill.Side == OrderSide.Buy)
        {
            if (value > _cash) throw new InvalidOperationException("Insufficient virtual cash.");
            _cash -= value;
            if (_positions.TryGetValue(fill.Symbol, out var existing))
            {
                var quantity = existing.Quantity + fill.Quantity;
                var average = ((existing.AverageEntryPrice * existing.Quantity) + value) / quantity;
                _positions[fill.Symbol] = existing with { Quantity = quantity, AverageEntryPrice = average };
            }
            else _positions[fill.Symbol] = new(Guid.NewGuid(), fill.Symbol, fill.Quantity, fill.Price, null);
            _marketPrices[fill.Symbol] = fill.Price;
        }
        else
        {
            if (!_positions.TryGetValue(fill.Symbol, out var position) || position.Quantity < fill.Quantity)
                throw new InvalidOperationException("Cannot sell more than the open position.");
            _cash += value;
            _realized += (fill.Price - position.AverageEntryPrice) * fill.Quantity;
            var remaining = position.Quantity - fill.Quantity;
            if (remaining == 0)
            {
                _positions.Remove(fill.Symbol);
                _marketPrices.Remove(fill.Symbol);
            }
            else _positions[fill.Symbol] = position with { Quantity = remaining };
        }
    }

    public void SetStopLoss(Guid positionId, decimal stopLoss)
    {
        var match = _positions.FirstOrDefault(x => x.Value.Id == positionId);
        if (match.Value is null) throw new KeyNotFoundException("Position not found.");
        _positions[match.Key] = match.Value with { StopLoss = stopLoss };
    }

    public void UpdateMarketPrice(Symbol symbol, decimal price)
    {
        if (_positions.ContainsKey(symbol)) _marketPrices[symbol] = price;
    }
}