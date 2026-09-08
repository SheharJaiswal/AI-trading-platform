using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AiTrading.Application;
using AiTrading.Domain;

namespace AiTrading.Infrastructure;

public sealed class AngelOneOptions
{
    public string BaseUrl { get; init; } = "https://apiconnect.angelone.in";
    public string ApiKey { get; init; } = "";
    public string AuthorizationToken { get; init; } = "";
}

public sealed class AngelOneMarketDataProvider(HttpClient http, AngelOneOptions options) : IMarketDataProvider
{
    private sealed record ApiResponse<T>(bool Status, string Message, string? Errorcode, T? Data);
    private sealed record QuoteEnvelope(List<QuoteData> Fetched, List<JsonElement>? Unfetched);
    private sealed record QuoteData(string Exchange, string TradingSymbol, string SymbolToken, decimal Ltp, decimal Open, decimal High, decimal Low, decimal Close, long TradeVolume, string? ExchFeedTime);
    private sealed record QuoteRequest(string Mode, Dictionary<string, string[]> ExchangeTokens);
    private sealed record CandleRequest(string Exchange, string Symboltoken, string Interval, string Fromdate, string Todate);

    public async Task<MarketQuote> GetQuoteAsync(Symbol symbol, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(symbol.InstrumentToken))
            throw new ArgumentException("An Angel One instrument token is required for market data.", nameof(symbol));

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{options.BaseUrl.TrimEnd('/')}/rest/secure/angelbroking/market/v1/quote/")
        {
            Content = JsonContent.Create(new QuoteRequest("FULL", new() { ["NSE"] = [symbol.InstrumentToken] }))
        };
        AddHeaders(request);
        var response = await http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<QuoteEnvelope>>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Angel One returned an empty response.");
        if (!payload.Status || payload.Data is null || payload.Data.Fetched.Count == 0)
            throw new InvalidOperationException($"Angel One quote failed: {payload.Message} {payload.Errorcode}");
        var data = payload.Data.Fetched[0];
        if (!DateTimeOffset.TryParse(data.ExchFeedTime, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var exchangeTime))
            throw new InvalidOperationException("Angel One quote did not contain a valid exchange feed timestamp.");
        return new(symbol, data.Exchange, data.SymbolToken, exchangeTime, data.Open, data.High, data.Low, data.Close, data.Ltp, data.TradeVolume, "angelOne");
    }

    public async Task<IReadOnlyList<Candle>> GetCandlesAsync(Symbol symbol, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(symbol.InstrumentToken))
            throw new ArgumentException("An Angel One instrument token is required for market data.", nameof(symbol));

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{options.BaseUrl.TrimEnd('/')}/rest/secure/angelbroking/historical/v1/getCandleData")
        {
            Content = JsonContent.Create(new CandleRequest("NSE", symbol.InstrumentToken, "ONE_DAY", from.ToString("yyyy-MM-dd HH:mm"), to.ToString("yyyy-MM-dd HH:mm")))
        };
        AddHeaders(request);
        var response = await http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<List<List<JsonElement>>>>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Angel One returned an empty candle response.");
        if (!payload.Status || payload.Data is null) throw new InvalidOperationException($"Angel One candles failed: {payload.Message} {payload.Errorcode}");
        return payload.Data.Select(ParseCandle).ToArray();
    }

    private void AddHeaders(HttpRequestMessage request)
    {
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Add("X-PrivateKey", options.ApiKey);
        request.Headers.Add("X-UserType", "USER");
        request.Headers.Add("X-SourceID", "WEB");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.AuthorizationToken);
    }

    private static Candle ParseCandle(List<JsonElement> row)
    {
        if (row.Count < 6) throw new InvalidOperationException("Invalid Angel One candle row.");
        if (!DateTimeOffset.TryParse(row[0].GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var timestamp))
            throw new InvalidOperationException("Invalid Angel One candle timestamp.");
        return new(timestamp, row[1].GetDecimal(), row[2].GetDecimal(), row[3].GetDecimal(), row[4].GetDecimal(), row[5].GetInt64());
    }
}

public sealed class InMemoryPaperExecutionProvider : IPaperExecutionProvider
{
    public Task<Fill> ExecuteAsync(PaperOrder order, CancellationToken cancellationToken) =>
        Task.FromResult(new Fill(order.Id, order.Symbol, order.Side, order.Quantity, order.LimitPrice, DateTimeOffset.UtcNow, "paper"));
}
