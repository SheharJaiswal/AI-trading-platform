using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

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
}
