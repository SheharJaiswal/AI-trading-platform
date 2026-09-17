using System.Net;
using System.Text.Json;
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
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("INVALID_LIMIT", body);
    }

    [Fact]
    public async Task HistoryEndpoint_Returns_BadRequest_For_NonNumeric_Limit()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseSetting("Persistence:MySql:Enabled", "false"));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/monitoring/runs?limit=abc");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("INVALID_LIMIT", body);
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
    public async Task DetailEndpoint_Returns_BadRequest_For_Empty_Id_With_Stable_Error_Code()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseSetting("Persistence:MySql:Enabled", "false"));
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/monitoring/runs/{Guid.Empty}");
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("INVALID_MONITORING_RUN_ID", document.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task DetailEndpoint_Returns_NotFound_For_Unknown_Id_With_Stable_Error_Code()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseSetting("Persistence:MySql:Enabled", "true"));
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/monitoring/runs/{Guid.NewGuid()}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("MONITORING_RUN_NOT_FOUND", body);
    }
}
