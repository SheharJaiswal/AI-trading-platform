using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using AiTrading.Domain;

namespace AiTrading.Application.Tests;

public sealed class ApiContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public ApiContractTests(WebApplicationFactory<Program> factory) => this.factory = factory;

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

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task MonitoringRunHistory_Rejects_NonPositive_Limit(int limit)
    {
        using var client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Persistence:MySql:Enabled", "true");
            builder.UseSetting("ConnectionStrings:MySql", "Server=localhost;Port=3306;Database=ai_trading_test;User=root;Password=test;");
        }).CreateClient();
        var response = await client.GetAsync($"/api/monitoring/runs?limit={limit}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("INVALID_LIMIT", document.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task MonitoringRunHistory_Rejects_Malformed_Limit()
    {
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/monitoring/runs?limit=not-a-number");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
            return Task.FromResult((new RiskResult(RiskDecision.Approved, null), (FillState?)new FillState(orderId, idempotencyKey, new Fill(orderId, symbol, OrderSide.Buy, quantity, 100m, DateTimeOffset.UtcNow))));
        }
    }
}
