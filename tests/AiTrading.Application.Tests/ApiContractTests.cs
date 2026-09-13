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
        Assert.True(document.RootElement.TryGetProperty("persistence", out var persistence));
        Assert.False(persistence.GetBoolean());
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

    [Fact]
    public async Task PaperSession_Event_Rejects_Route_Request_Id_Mismatch()
    {
        using var client = factory.CreateClient();
        var routeId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        using var content = new StringContent($"{{\"sessionId\":\"{requestId}\",\"symbol\":{{\"value\":\"TCS\"}},\"quantity\":1,\"eventId\":\"evt-1\"}}", System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync($"/api/paper-sessions/{routeId}/events", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("SESSION_ID_MISMATCH", document.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task PaperSession_Event_Requires_Durable_Persistence()
    {
        using var client = factory.CreateClient();
        var sessionId = Guid.NewGuid();
        using var content = new StringContent($"{{\"sessionId\":\"{sessionId}\",\"symbol\":{{\"value\":\"TCS\"}},\"quantity\":1,\"eventId\":\"evt-1\"}}", System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync($"/api/paper-sessions/{sessionId}/events", content);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("PERSISTENCE_DISABLED", document.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task MonitoringRunHistory_Requires_Durable_Persistence()
    {
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/monitoring/runs");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("PERSISTENCE_DISABLED", document.RootElement.GetProperty("errorCode").GetString());
        Assert.Equal("Monitoring-run history requires MySQL persistence.", document.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task RecoveryDiagnostic_Requires_Durable_Persistence()
    {
        using var client = factory.CreateClient();
        var positionId = Guid.NewGuid();
        var response = await client.GetAsync($"/api/paper-shorts/{positionId}/recovery");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("PERSISTENCE_DISABLED", document.RootElement.GetProperty("errorCode").GetString());
        Assert.Equal("Durable short recovery diagnostics require MySQL persistence.", document.RootElement.GetProperty("message").GetString());
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
            return Task.FromResult<(RiskResult Risk, FillState? Fill)>((new RiskResult(RiskDecision.Approved, null), new FillState(Guid.NewGuid(), orderId, symbol, OrderSide.Buy, quantity, 100m, DateTimeOffset.UtcNow, "fake")));
        }
    }
}
