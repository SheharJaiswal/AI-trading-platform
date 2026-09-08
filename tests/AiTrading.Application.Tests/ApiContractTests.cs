using System.Net;
using System.Text.Json;
using AiTrading.Application;
using AiTrading.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
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
    public async Task PaperTrade_Uses_Stable_Order_Id_For_Idempotency_Key()
    {
        var fake = new FakePaperTradeService();
        using var client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Persistence:MySql:Enabled", "true");
            builder.UseSetting("ConnectionStrings:MySql", "Server=localhost;Port=3306;Database=ai_trading_test;User=root;Password=test;");
            builder.UseSetting("Trading:PortfolioId", "00000000-0000-0000-0000-000000000001");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IPaperTradeService>();
                services.AddSingleton<IPaperTradeService>(fake);
            });
        }).CreateClient();

        using var first = new HttpRequestMessage(HttpMethod.Post, "/api/paper-trades/TCS?quantity=1");
        first.Headers.Add("Idempotency-Key", "api-request-123");
        using var firstResponse = await client.SendAsync(first);
        using var second = new HttpRequestMessage(HttpMethod.Post, "/api/paper-trades/TCS?quantity=1");
        second.Headers.Add("Idempotency-Key", "api-request-123");
        using var secondResponse = await client.SendAsync(second);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        Assert.Equal(2, fake.ExecutionCount);
        Assert.Equal("api-request-123", fake.LastIdempotencyKey);
        Assert.NotNull(fake.FirstOrderId);
        Assert.Equal(fake.FirstOrderId, fake.LastOrderId);
    }

    private sealed class FakePaperTradeService : IPaperTradeService
    {
        public int ExecutionCount { get; private set; }
        public string? LastIdempotencyKey { get; private set; }
        public Guid? FirstOrderId { get; private set; }
        public Guid? LastOrderId { get; private set; }

        public Task<(RiskResult Risk, FillState? Fill)> ExecuteAsync(Guid portfolioId, Guid orderId, string idempotencyKey, Symbol symbol, int quantity, CancellationToken cancellationToken)
        {
            FirstOrderId ??= orderId;
            LastOrderId = orderId;
            LastIdempotencyKey = idempotencyKey;
            ExecutionCount++;
            return Task.FromResult<(RiskResult Risk, FillState? Fill)>((new RiskResult(RiskDecision.Approved, null), new FillState(orderId, symbol, quantity, 100m)));
        }
    }
}
