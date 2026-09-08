using System.Net;
using System.Net.Http;
using System.Text;
using AiTrading.Domain;
using AiTrading.Infrastructure;

namespace AiTrading.Application.Tests;

public class AngelOneMarketDataProviderTests
{
    [Fact]
    public async Task Quote_Fixture_Is_Mapped_To_Normalized_Contract()
    {
        var handler = new FixtureHandler(LoadFixture("angelone-quote.json"));
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://test.local") };
        var provider = new AngelOneMarketDataProvider(http, new AngelOneOptions
        {
            BaseUrl = "https://test.local",
            ApiKey = "test-key",
            AuthorizationToken = "test-token"
        });

        var result = await provider.GetQuoteAsync(new Symbol("TCS", "11536"), CancellationToken.None);

        Assert.Equal("NSE", result.Exchange);
        Assert.Equal("11536", result.InstrumentToken);
        Assert.Equal(3501.25m, result.LastTradedPrice);
        Assert.Equal("angelOne", result.Source);
        Assert.Equal(new DateTimeOffset(2026, 9, 8, 4, 15, 0, TimeSpan.Zero), result.Timestamp);
        Assert.False(string.IsNullOrWhiteSpace(handler.LastAuthorization));
        Assert.Equal("test-key", handler.LastPrivateKey);
    }

    [Fact]
    public async Task Candle_Fixture_Is_Mapped_Without_Provider_Dtos()
    {
        var handler = new FixtureHandler(LoadFixture("angelone-candles.json"));
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://test.local") };
        var provider = new AngelOneMarketDataProvider(http, new AngelOneOptions
        {
            BaseUrl = "https://test.local",
            ApiKey = "test-key",
            AuthorizationToken = "test-token"
        });

        var result = await provider.GetCandlesAsync(
            new Symbol("TCS", "11536"),
            DateTimeOffset.Parse("2026-09-07T00:00:00+05:30"),
            DateTimeOffset.Parse("2026-09-08T00:00:00+05:30"),
            CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal(100m, result[0].Open);
        Assert.Equal(104m, result[1].Close);
        Assert.Equal(12000, result[1].Volume);
    }

    private static string LoadFixture(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));

    private sealed class FixtureHandler(string payload) : HttpMessageHandler
    {
        public string? LastAuthorization { get; private set; }
        public string? LastPrivateKey { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastAuthorization = request.Headers.Authorization?.ToString();
            LastPrivateKey = request.Headers.TryGetValues("X-PrivateKey", out var values) ? values.Single() : null;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            });
        }
    }
}
