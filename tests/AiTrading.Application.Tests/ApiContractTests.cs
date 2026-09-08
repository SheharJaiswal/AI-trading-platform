using System.Net;
using System.Text.Json;
using AiTrading.Application;
using AiTrading.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AiTrading.Application.Tests;

public class ApiContractTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Health_Returns_Stable_Paper_Mode_Contract()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");
        response.EnsureSuccessStatusCode();
        Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("ok", document.RootElement.GetProperty("status").GetString());
        Assert.Equal("paper", document.RootElement.GetProperty("mode").GetString());
        Assert.True(document.RootElement.TryGetProperty("marketProvider", out _));
    }

    [Fact]
    public async Task Portfolio_Returns_Stable_Json_Contract()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/portfolio");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(document.RootElement.TryGetProperty("cash", out _));
        Assert.True(document.RootElement.TryGetProperty("positions", out _));
        Assert.True(document.RootElement.TryGetProperty("unrealizedPnl", out _));
        Assert.True(document.RootElement.TryGetProperty("realizedPnl", out _));
    }

    [Fact]
    public async Task PaperTrade_Requires_Idempotency_Key()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/api/paper-trades/TCS?quantity=1", content: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("INVALID_IDEMPOTENCY_KEY", document.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task PaperTrade_Preserves_Idempotency_Key_Contract()
    {
        var fake = new FakePaperTradeService();
        using var client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Persistence:MySql:Enabled"] = "false"
            }));
            builder.ConfigureServices(services => services.AddSingleton<IPaperTradeService>(fake));
        }).CreateClient();

        using var first = new HttpRequestMessage(HttpMethod.Post, "/api/paper-trades/TCS?quantity=1");
        first.Headers.Add("Idempotency-Key", "api-request-123");
        using var firstResponse = await client.SendAsync(first);
        using var second = new HttpRequestMessage(HttpMethod.Post, "/api/paper-trades/TCS?quantity=1");
        second.Headers.Add("Idempotency-Key", "api-request-123");
        using var secondResponse = await client.SendAsync(second);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, secondResponse.StatusCode);
        Assert.Equal(0, fake.ExecutionCount);
        Assert.Null(fake.LastIdempotencyKey);
    }

    private sealed class FakePaperTradeService : IPaperTradeService
    {
        public int ExecutionCount { get; private set; }
        public string? LastIdempotencyKey { get; private set; }

        public Task<(RiskResult Risk, FillState? Fill)> ExecuteAsync(Guid portfolioId, Guid orderId, string idempotencyKey, Symbol symbol, int quantity, CancellationToken cancellationToken)
        {
            LastIdempotencyKey = idempotencyKey;
            ExecutionCount++;
            return Task.FromResult<(RiskResult Risk, FillState? Fill)>((new RiskResult(RiskDecision.Approved, null), null));
        }
    }
}
