using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AiTrading.Application.Tests;

public sealed class MonitoringRunEndpointTests
{
    [Fact]
    public async Task HistoryEndpoint_Returns_ServiceUnavailable_WhenPersistenceDisabled()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseSetting("Persistence:MySql:Enabled", "false"));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/monitoring/runs");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }
}
