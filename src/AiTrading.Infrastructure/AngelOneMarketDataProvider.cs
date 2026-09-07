using System.Net.Http.Headers;
using System.Net.Http.Json;
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
    private sealed record QuoteData(decimal Ltp, decimal Open, decimal High, decimal Low, decimal Close, long TradeVolume, string ExchangeFeedTime);
    private sealed record QuoteRequest(string Mode, Dictionary<string, string[]> ExchangeTokens);
    private sealed record CandleRequest(string Exchange, string Symboltoken, string Interval, string Fromdate, string Todate);

    public async Task<MarketQuote> GetQuoteAsync(Symbol symbol, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{options.BaseUrl.TrimEnd('/')}/rest/secure/angelbroking/market/v1/quote/")
        {
            Content = JsonContent.Create(new QuoteRequest("FULL", new() { ["NSE"] = [symbol.Value] }))
        };
        AddHeaders(request);
        var response = await http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<Dictionary<string, QuoteData>>>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Angel One returned an empty response.");
        if (!payload.Status || payload.Data is null || !payload.Data.TryGetValue(symbol.Value, out var data))
            throw new InvalidOperationException($"Angel One quote failed: {payload.Message} {payload.Errorcode}");
        return new(symbol, "NSE", symbol.Value, DateTimeOffset.UtcNow, data.Open, data.High, data.Low, data.Ltp, data.TradeVolume, "angelOne");
    }

    public async Task<IReadOnlyList<Candle>> GetCandlesAsync(Symbol symbol, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{options.BaseUrl.TrimEnd('/')}/rest/secure/angelbroking/historical/v1/getCandleData")
        {
            Content = JsonContent.Create(new CandleRequest("NSE", symbol.Value, "ONE_DAY", from.ToString("yyyy-MM-dd HH:mm"), to.ToString("yyyy-MM-dd HH:mm")))
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
        request.Headers.Add("X-SourceID", "WEB");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.AuthorizationToken);
    }

    private static Candle ParseCandle(List<JsonElement> row)
    {
        if (row.Count < 6) throw new InvalidOperationException("Invalid Angel One candle row.");
        return new(DateTimeOffset.Parse(row[0].GetString()!), row[1].GetDecimal(), row[2].GetDecimal(), row[3].GetDecimal(), row[4].GetDecimal(), row[5].GetInt64());
    }
}

public sealed class InMemoryPaperExecutionProvider : IPaperExecutionProvider
{
    public Task<Fill> ExecuteAsync(PaperOrder order, CancellationToken cancellationToken) =>
        Task.FromResult(new Fill(order.Id, order.Symbol, order.Side, order.Quantity, order.LimitPrice, DateTimeOffset.UtcNow, "paper"));
}
