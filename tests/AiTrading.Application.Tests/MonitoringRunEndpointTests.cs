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

    [Fact]
    public async Task HistoryEndpoint_Returns_BadRequest_For_NonPositive_Limit()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseSetting("Persistence:MySql:Enabled", "false"));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/monitoring/runs?limit=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task HistoryEndpoint_Returns_BadRequest_For_NonNumeric_Limit()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseSetting("Persistence:MySql:Enabled", "false"));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/monitoring/runs?limit=abc");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DetailEndpoint_Returns_ServiceUnavailable_WhenPersistenceDisabled()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseSetting("Persistence:MySql:Enabled", "false"));
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/monitoring/runs/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task DetailEndpoint_Returns_BadRequest_For_Empty_Id()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseSetting("Persistence:MySql:Enabled", "false"));
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/monitoring/runs/{Guid.Empty}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
