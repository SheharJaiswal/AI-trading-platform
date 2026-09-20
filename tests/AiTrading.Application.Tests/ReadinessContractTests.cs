using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AiTrading.Application.Tests;

public sealed class ReadinessContractTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Ready_Returns_ServiceUnavailable_When_Persistence_Cannot_Connect()
    {
        using var client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Persistence:MySql:Enabled", "true");
            builder.UseSetting("ConnectionStrings:MySql", "Server=127.0.0.1;Port=1;Database=ai_trading_test;User=root;Password=test;Connection Timeout=1;");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IHostedService>();
            });
        }).CreateClient();

        using var response = await client.GetAsync("/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"status\":\"not_ready\"", body, StringComparison.Ordinal);
        Assert.Contains("\"persistence\":true", body, StringComparison.Ordinal);
    }
}
