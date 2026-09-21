using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace AiTrading.Application.Tests;

public sealed class PortfolioStartupMigrationTests
{
    [Fact]
    public async Task Persistence_startup_applies_migrations_before_portfolio_initialization()
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Persistence:MySql:Enabled", "true");
            builder.UseSetting("ConnectionStrings:MySql", "Server=localhost;Port=3306;Database=ai_trading_test;User=root;Password=test;");
            builder.UseSetting("Trading:PortfolioId", "00000000-0000-0000-0000-000000000001");
        });

        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/portfolio");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(1_000_000m, document.RootElement.GetProperty("cash").GetDecimal());
        Assert.True(document.RootElement.TryGetProperty("positions", out _));
    }
}
